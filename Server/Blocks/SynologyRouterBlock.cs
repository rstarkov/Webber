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

public record SynologyRouterBlockDto : BaseDto
{
    public record TxRxPair
    {
        public double TxRate { get; set; }
        public double RxRate { get; set; }
    }

    public record TxRxDevicePair : TxRxPair
    {
        public string DeviceName { get; set; }
    }

    public double Rx { get; set; }
    public double Tx { get; set; }
    public TxRxPair[] History { get; set; }
    public TxRxDevicePair[] TopDevices { get; set; }
}

class SynologyRouterBlockServer : SimpleBlockServerBase<SynologyRouterBlockDto>
{
    private SynologySrmService _srm;

    public SynologyRouterBlockServer(IServiceProvider sp, SynologyRouterBlockConfig config)
        : base(sp, config.QueryIntervalMs)
    {
        _srm = new SynologySrmService(config.BaseUrl, config.Port, config.Https, config.LoginUser, config.LoginPassword);
    }

    protected override SynologyRouterBlockDto Tick()
    {
        var traffic = _srm.GetNgfwTraffic().GetAwaiter().GetResult();

        var totalrx = traffic.Sum(k => k.Download);
        var totaltx = traffic.Sum(k => k.Upload);

        var dto = new SynologyRouterBlockDto()
        {
            Tx = totaltx,
            Rx = totalrx,
        };

        return dto;
    }
}
