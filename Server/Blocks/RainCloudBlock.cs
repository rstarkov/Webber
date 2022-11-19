using RT.Serialization;
using RT.Util.ExtensionMethods;
using SkiaSharp;
using Webber.Client.Models;
using Webber.Server.Services;

namespace Webber.Server.Blocks;

class RainCloudBlockConfig
{
    public double LocationX { get; set; }
    public double LocationY { get; set; }
    public string CachePath { get; set; } = null;
}

class RainCloudBlockServer : SimpleBlockServerBase<RainCloudBlockDto>
{
    private RainCloudBlockConfig _config;
    private MetOfficeMapsService _metoffice;
    private Dictionary<string, RainCloudPtDto> _havePoints = new();

    private Dictionary<SKColor, int> _cRain = new()
    {
        [new SKColor(0x00000000)] = 0,
        [new SKColor(0xff0000fe)] = 1,
        [new SKColor(0xff3265fe)] = 2,
        [new SKColor(0xff0cbcfe)] = 3,
        [new SKColor(0xff00a300)] = 4,
        [new SKColor(0xfffecb00)] = 5,
        [new SKColor(0xfffe9800)] = 6,
        [new SKColor(0xfffe0000)] = 7,
        [new SKColor(0xffb30000)] = 8,
    };
    private Dictionary<SKColor, int> _cCloud = new()
    {
        [new SKColor(0x00000000)] = 0,
        [new SKColor(0x20efefef)] = 1,
        [new SKColor(0x40efefef)] = 2,
        [new SKColor(0x60efefef)] = 3,
        [new SKColor(0x80ececec)] = 4,
        [new SKColor(0xa0ececec)] = 5,
        [new SKColor(0xc0ededed)] = 6,
        [new SKColor(0xefededed)] = 7,
        [new SKColor(0xf0ededed)] = 8,
    };

    private class Pt
    {
        public MetOfficeMapsService.Timestep Step;
        public RainCloudPtDto Dto;
    }

    public RainCloudBlockServer(IServiceProvider sp, RainCloudBlockConfig config)
        : base(sp, TimeSpan.FromMinutes(5))
    {
        _config = config;
        _metoffice = new MetOfficeMapsService();
    }

    public override void Start()
    {
        if (_config.CachePath != null)
            if (File.Exists(_config.CachePath))
                _havePoints = ClassifyXml.DeserializeFile<Dictionary<string, RainCloudPtDto>>(_config.CachePath);
        base.Start();
    }

    protected override bool ShouldTick() => true;

    protected override RainCloudBlockDto Tick()
    {
        _metoffice.Refresh();

        var rainPast = _metoffice.Models["rainfall_radar"].Timesteps.Where(ts => ts.Time >= DateTime.Now.AddHours(-24)).ToList();
        var rainFore = _metoffice.Models["total_precipitation_rate"].Timesteps.Where(ts => ts.Time <= DateTime.Now.AddHours(48)).ToList();
        var cloudFore = _metoffice.Models["cloud_amount_total"].Timesteps.Where(ts => ts.Time <= DateTime.Now.AddHours(48)).ToList();

        var newPoints = new Dictionary<string, RainCloudPtDto>();

        var dto = new RainCloudBlockDto { ValidUntilUtc = DateTime.UtcNow + TimeSpan.FromMinutes(30) };
        var maxPast = rainPast.Max(r => r.Time);
        dto.Rain = rainPast.Select(p => GetPt(p, newPoints, _cRain, false)).Concat(rainFore.Where(r => r.Time > maxPast).Select(p => GetPt(p, newPoints, _cRain, true))).ToArray();
        dto.Cloud = cloudFore.Select(p => GetPt(p, newPoints, _cCloud, true)).ToArray();

        _havePoints = newPoints;
        if (_config.CachePath != null)
            ClassifyXml.SerializeToFile(_havePoints, _config.CachePath);

        return dto;
    }

    private RainCloudPtDto GetPt(MetOfficeMapsService.Timestep ts, Dictionary<string, RainCloudPtDto> newPoints, Dictionary<SKColor, int> colormap, bool isForecast)
    {
        RainCloudPtDto result;

        if (_havePoints.ContainsKey(ts.Url))
            result = _havePoints[ts.Url];
        else
        {
            result = new RainCloudPtDto { AtUtc = ts.Time, Counts = null, IsForecast = isForecast };
            // download and decode bitmap
            SKBitmap bmp = null;
            try
            {
                var bytes = _metoffice.DownloadImage(ts).GetAwaiter().GetResult();
                Thread.Sleep(200);
                bmp = SKBitmap.Decode(bytes);
            }
            catch { } // download and decode can fail for various benign reasons; expect and ignore this, we'll retry later

            // count forecast pixels - errors here should propagate so that we know about them
            if (bmp != null)
            {
                int x = (int)Math.Round(bmp.Width * _config.LocationX);
                int y = (int)Math.Round(bmp.Height * _config.LocationY);
                result.Counts = new int[colormap.Values.Max() + 1];
                for (int cy = y - 1; cy <= y + 1; cy++)
                for (int cx = x - 1; cx <= x + 1; cx++)
                {
                    var clr = bmp.GetPixel(cx, cy);
                    var best = colormap
                        .Select(m => (m, diff: Math.Abs(m.Key.Alpha - clr.Alpha) + Math.Abs(m.Key.Red - clr.Red) + Math.Abs(m.Key.Green - clr.Green) + Math.Abs(m.Key.Blue - clr.Blue)))
                        .MinElement(el => el.diff);
                    if (best.diff > 6)
                        throw new Exception();
                    result.Counts[best.m.Value]++;
                }
            }
        }

        if (result.Counts != null) // cache only successful results so that we retry failures on next refresh
            newPoints[ts.Url] = result;
        return result;
    }
}
