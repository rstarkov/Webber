﻿using System.IO.Compression;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using RT.Util.ExtensionMethods;
using Webber.Client.Models;

namespace Webber.Server.Blocks;

class WeatherDotComBlockConfig
{
    public string ApiKey { get; set; }
    public double Longitude { get; set; } // degrees, east is positive
    public double Latitude { get; set; } // degrees, north is positive
    public string HourlyForecastType { get; set; } = "2day"; // null to disable, "2day" or "15day"
    public string DumpPath { get; set; } = null; // optional but needed to reload recent forecasts on server restart. There is currently no way to delete old dumps from storage
}

class WeatherDotComBlockServer : SimpleBlockServerBase<WeatherDotComBlockDto>
{
    private WeatherDotComBlockConfig _config;
    private Dictionary<DateTime, WeatherDotComForecastHourDto> _recentHourly = new();
    private HttpClient _hc = new();

    public WeatherDotComBlockServer(IServiceProvider sp, WeatherDotComBlockConfig config) : base(sp, 0)
    {
        _config = config;
    }

    private string LocStr => $"{_config.Latitude:0.########},{_config.Longitude:0.########}";

    protected override WeatherDotComBlockDto Tick()
    {
        try { return FetchData(); }
        catch { Thread.Sleep(TimeSpan.FromMinutes(4)); }
        // Retry once on failure
        return FetchData();
    }

    private WeatherDotComBlockDto FetchData()
    {
        var url = $"https://api.weather.com/v3/wx/forecast/hourly/{_config.HourlyForecastType}?apiKey={_config.ApiKey}&format=json&language=en-US&units=m&geocode={LocStr}";
        var result = _hc.GetByteArrayAsync(url).GetAwaiter().GetResult();
        if (_config.DumpPath != null)
        {
            var now = DateTime.UtcNow;
            var fndate = $"{now:yyyy'-'MM'-'dd'T'HH'.'mm'.'ss'Z'}--ux{now.ToUnixSeconds()}";
            using var fs = File.Open(Path.Combine(_config.DumpPath, @$"hourly--{LocStr}--{fndate}--{_config.HourlyForecastType}.json.gz"), FileMode.Create, FileAccess.Write, FileShare.Read);
            using var gz = new GZipStream(fs, CompressionLevel.Optimal);
            gz.Write(result);
        }
        var json = JObject.Parse(result.FromUtf8());
        AddForecast(json);
        _recentHourly.RemoveAllByKey(k => k < DateTime.UtcNow.AddHours(-24));
        var hours = _recentHourly.Values.OrderBy(h => h.DateTime).ToArray();
        return new WeatherDotComBlockDto { Hours = hours, ValidUntilUtc = DateTime.UtcNow.AddMinutes(90) };
    }

    protected override void SleepUntilNextTick(DateTime tickStartUtc)
    {
        var next = new DateTime(tickStartUtc.Year, tickStartUtc.Month, tickStartUtc.Day, tickStartUtc.Hour, 25, 0, 0, DateTimeKind.Utc);
        if (next < tickStartUtc.AddMinutes(5))
            next = next.AddHours(1);
        Util.SleepUntil(next);
    }

    public override void Start()
    {
        if (_config.DumpPath != null)
        {
            var files = new DirectoryInfo(_config.DumpPath).GetFiles($"hourly--{LocStr}--*.json.gz");
            foreach (var file in files.OrderBy(f => f.Name)) // name sort is date sort
            {
                var ts = DateTimeOffset.FromUnixTimeSeconds(int.Parse(Regex.Match(file.Name, @"--ux(\d+)--").Groups[1].ValueSpan)).UtcDateTime;
                if (ts < DateTime.UtcNow.AddDays(-1))
                    continue;
                using var fs = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var gz = new GZipStream(fs, CompressionMode.Decompress);
                AddForecast(JObject.Parse(gz.ReadAllBytes().FromUtf8()));
            }
        }
        base.Start();
    }

    private void AddForecast(JObject json)
    {
        var times = (JArray)json["validTimeUtc"];
        var cloudCover = (JArray)json["cloudCover"];
        var precipChance = (JArray)json["precipChance"];
        var precipMm = (JArray)json["qpf"];

        for (int i = 0; i < times.Count; i++)
        {
            var hour = new WeatherDotComForecastHourDto();
            hour.DateTime = DateTimeOffset.FromUnixTimeSeconds(times[i].Value<int>()).UtcDateTime;
            hour.CloudCover = cloudCover[i].Value<int>();
            hour.PrecipChance = precipChance[i].Value<int>();
            hour.PrecipMm = precipMm[i].Value<double>();
            _recentHourly[hour.DateTime] = hour;
        }
    }
}
