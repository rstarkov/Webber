using RT.Util.ExtensionMethods;
using Webber.Client.Models;

namespace Webber.Server.Blocks;

class SynologyRouterBlockConfig
{
    public int Port { get; set; } = 8000;
    public bool Https { get; set; } = false;
    public string LoginUser { get; set; }
    public string LoginPassword { get; set; }
    public int TxEstimated { get; set; } // in bytes
    public int RxEstimated { get; set; } // in bytes
    public string BaseUrl { get; set; } = "192.168.1.1";
    public int QueryIntervalMs { get; set; } = 3000;
    public double AverageDecay { get; set; } = 0.85;
    public double AverageDecayFast { get; set; } = 0.50;
    public int RecentHistory { get; set; } = 30;
}

public record TxRxPair
{
    public DateTime Timestamp { get; set; }
    public double TxRate { get; set; }
    public double RxRate { get; set; }
}

public record TxRxDevicePairGroup
{
    public DateTime Timestamp { get; set; }
    public TxRxDevicePair[] Pairs { get; set; }
}

public record TxRxDevicePair : TxRxPair
{
    public string DeviceId { get; set; }
    public string DeviceIpv4 { get; set; }
    public string DeviceHostname { get; set; }
    public bool IsWireless { get; set; }
}

public record SynologyRouterBlockDto : BaseDto
{
    public double Rx { get; set; }
    public double RxMax { get; set; }
    public double[] RxHistory { get; set; }
    public double Tx { get; set; }
    public double TxMax { get; set; }
    public double[] TxHistory { get; set; }
    public TxRxDevicePairGroup TopDevices { get; set; }
    public bool Wan1 { get; set; }
    public bool Wan2 { get; set; }
    public int WifiClientCount { get; set; }
    public int LanClientCount { get; set; }
}

class SynologyRouterBlockServer : SimpleBlockServerBase<SynologyRouterBlockDto>
{
    private SynologySrmService _srm;
    private SynologyRouterBlockConfig _config;

    Queue<TxRxDevicePairGroup> _devices = new Queue<TxRxDevicePairGroup>();

    public SynologyRouterBlockServer(IServiceProvider sp, SynologyRouterBlockConfig config)
        : base(sp, config.QueryIntervalMs)
    {
        _config = config;
        _srm = new SynologySrmService(config.BaseUrl, config.Port, config.Https, config.LoginUser, config.LoginPassword);
    }

    protected override SynologyRouterBlockDto Tick()
    {
        var barSize = TimeSpan.FromSeconds(10);
        var historySize = _config.RecentHistory;
        var keepNum = historySize * ((int)barSize.TotalMilliseconds / _config.QueryIntervalMs);

        var routerResponse = _srm.GetCompoundStatus().GetAwaiter().GetResult();
        var traffic = routerResponse.DeviceTraffic;
        var devices = routerResponse.Devices;
        var gateways = routerResponse.Gateways;

        SynologySmartWanGateway wan1 = gateways.List.Where(g => g.InterfaceName == "ppp0").FirstOrDefault();
        SynologySmartWanGateway wan2 = gateways.List.Where(g => g.InterfaceName == "ppp1").FirstOrDefault();
        bool wan1Enabled = wan1?.NetStatus?.EqualsIgnoreCase("enabled") ?? false;
        bool wan2Enabled = wan2?.NetStatus?.EqualsIgnoreCase("enabled") ?? false;
        var lanDevices = devices.Devices.Where(d => !d.IsWireless).Count();
        var wifiDevices = devices.Devices.Where(d => d.IsWireless).Count();

        var time = DateTime.UtcNow;

        var pairs = from dev in devices.Devices
                    let tr = traffic.FirstOrDefault(traffic => traffic.DeviceID.EqualsIgnoreCase(dev.Mac))
                    where tr != null
                    select new TxRxDevicePair
                    {
                        DeviceHostname = dev.Hostname,
                        DeviceId = dev.Mac,
                        DeviceIpv4 = dev.IpAddr,
                        RxRate = tr.Download,
                        TxRate = tr.Upload,
                        Timestamp = time,
                        IsWireless = dev.IsWireless,
                    };

        var pairsArr = pairs.ToArray();
        var devicesCache = _devices.EnqueueWithMaxCapacity(new TxRxDevicePairGroup { Pairs = pairsArr, Timestamp = time }, keepNum);
        var barSizeGrouping = from pairGroup in devicesCache
                              group pairGroup by (int)(pairGroup.Timestamp.ToUnixSeconds() / (int)barSize.TotalSeconds) into g
                              orderby g.Key descending
                              select g;

        var totalByBarSize = from g in barSizeGrouping
                             let dev = g.SelectMany(z => z.Pairs)
                             select new TxRxPair { TxRate = dev.Max(t => t.TxRate), RxRate = dev.Max(t => t.RxRate) };

        var totalByBarSizeArr = totalByBarSize.Take(historySize).ToArray();
        Array.Reverse(totalByBarSizeArr);
        Array.Resize(ref totalByBarSizeArr, historySize);

        var topDevices = from g in barSizeGrouping
                         let dev = g.SelectMany(z => z.Pairs)
                         let d = (
                             from d in dev
                             group d by d.DeviceId into g2
                             let id = g2.Key
                             let firstPair = g2.OrderByDescending(z => z.Timestamp).First()
                             let combined = firstPair with { RxRate = g2.Max(t => t.RxRate), TxRate = g2.Max(t => t.TxRate) }
                             let txPerc = combined.TxRate / _config.TxEstimated
                             let rxPerc = combined.RxRate / _config.RxEstimated
                             let utilMax = Math.Min(1, Math.Max(txPerc, rxPerc))
                             orderby utilMax descending
                             select combined
                         )
                         select new TxRxDevicePairGroup { Pairs = d.Take(3).ToArray(), Timestamp = d.First().Timestamp };

        //var topDevicesArr = topDevices.Take(historySize).ToArray();
        //Array.Reverse(topDevicesArr);
        //Array.Resize(ref topDevicesArr, historySize);

        var totalrx = traffic.Sum(k => k.Download);
        var totaltx = traffic.Sum(k => k.Upload);

        var dto = new SynologyRouterBlockDto()
        {
            Tx = totaltx,
            TxMax = _config.TxEstimated,
            TxHistory = totalByBarSizeArr.Select(t => t?.TxRate ?? 0).ToArray(),
            Rx = totalrx,
            RxMax = _config.RxEstimated,
            RxHistory = totalByBarSizeArr.Select(t => t?.RxRate ?? 0).ToArray(),
            TopDevices = topDevices.First(),
            Wan1 = wan1Enabled,
            Wan2 = wan2Enabled,
            LanClientCount = lanDevices,
            WifiClientCount = wifiDevices,
        };

        return dto;
    }
}
