using Webber.Client.Models;

namespace Webber.Server.Blocks;

class SynologyRouterBlockConfig : RouterBlockConfig
{
    public int Port { get; set; } = 8000;
    public bool Https { get; set; } = false;
    public string LoginUser { get; set; }
    public string LoginPassword { get; set; }
    public int TxEstimated { get; set; } // in bytes
    public int RxEstimated { get; set; } // in bytes
}

public record SynologyRouterBlockDto : RouterBlockDto
{

}

class SynologyRouterBlockServer : RouterBlockServerBase<SynologyRouterBlockDto, SynologyRouterBlockConfig>
{
    private SynologySrmService _srm;

    public SynologyRouterBlockServer(IServiceProvider sp, SynologyRouterBlockConfig config, IDbService db)
        : base(sp, config, db)
    {
        _srm = new SynologySrmService(config.BaseUrl, config.Port, config.Https, config.LoginUser, config.LoginPassword);
    }

    protected override SynologyRouterBlockDto Tick()
    {
        var traffic = _srm.GetNgfwTraffic().GetAwaiter().GetResult();

        var pt = new RouterHistoryPoint();
        pt.Timestamp = DateTime.UtcNow;
        pt.RxTotal = traffic.Sum(k => k.Download);
        pt.TxTotal = traffic.Sum(k => k.Upload);

        var dto = ProcessHistoryPoint(pt);

        // new stuff here

        return dto;
    }
}
