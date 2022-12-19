using Dapper;
using Dapper.Contrib.Extensions;
using RT.Util.ExtensionMethods;
using Webber.Client.Models;

namespace Webber.Server.Blocks;

class RouterBlockConfig
{
    public string BaseUrl { get; set; } = "http://192.168.1.1";
    public int QueryIntervalMs { get; set; } = 3000;
    public double AverageDecay { get; set; } = 0.85;
    public double AverageDecayFast { get; set; } = 0.50;
}

abstract class RouterBlockServerBase<TDto, TConfig> : SimpleBlockServerBase<TDto>
    where TDto : RouterBlockDto, new()
    where TConfig : RouterBlockConfig
{
    protected TConfig Config => _config;

    private TConfig _config;
    private IDbService _db;
    private Queue<RouterHistoryPoint> _history = new Queue<RouterHistoryPoint>();

    public RouterBlockServerBase(IServiceProvider sp, TConfig config, IDbService db)
        : base(sp, config.QueryIntervalMs)
    {
        _config = config;
        _db = db;
        registerMigrations();
    }

    public override void Start()
    {
        if (_db.Enabled)
            using (var conn = _db.OpenConnection())
            {
                _history = conn.Query<TbRouterHistoryEntry>(
                        $@"SELECT * FROM {nameof(TbRouterHistoryEntry)} WHERE {nameof(TbRouterHistoryEntry.Timestamp)} >= @limit ORDER BY {nameof(TbRouterHistoryEntry.Timestamp)}",
                        new { limit = DateTime.UtcNow.AddHours(-24).ToDbDateTime() }
                    )
                    .Select(pt => new RouterHistoryPoint { Timestamp = pt.Timestamp.FromDbDateTime(), TxTotal = pt.TxTotal, RxTotal = pt.RxTotal })
                    .ToQueue();
            }
        ptPrev = _history.LastOrDefault();

        base.Start();
    }

    protected override bool ShouldTick() => true;
    RouterHistoryPoint ptPrev;
    double avgRx = 0;
    double avgTx = 0;
    double avgRxFast = 0;
    double avgTxFast = 0;
    Queue<(double txRate, double rxRate)> recentHistory = new();

    protected virtual TDto ProcessHistoryPoint(RouterHistoryPoint pt)
    {
        _history.Enqueue(pt);
        while (_history.Peek().Timestamp < DateTime.UtcNow.AddHours(-24))
            _history.Dequeue();

        if (ptPrev == null)
        {
            ptPrev = pt;
            return null;
        }

        while (pt.TxTotal < ptPrev.TxTotal)
            pt.TxTotal += uint.MaxValue;
        while (pt.RxTotal < ptPrev.RxTotal)
            pt.RxTotal += uint.MaxValue;
        var txDiff = pt.TxTotal - ptPrev.TxTotal;
        var rxDiff = pt.RxTotal - ptPrev.RxTotal;
        var timeDiff = (pt.Timestamp - ptPrev.Timestamp).TotalSeconds;

        if (_db.Enabled)
            using (var conn = _db.OpenConnection())
                conn.Insert(new TbRouterHistoryEntry { Timestamp = pt.Timestamp.ToDbDateTime(), TxTotal = pt.TxTotal, RxTotal = pt.RxTotal });

        var rxRate = rxDiff / timeDiff;
        var txRate = txDiff / timeDiff;

        recentHistory.Enqueue((txRate: txRate, rxRate: rxRate));
        while (recentHistory.Count > 24)
            recentHistory.Dequeue();

        avgRx = avgRx * _config.AverageDecay + rxRate * (1 - _config.AverageDecay);
        avgTx = avgTx * _config.AverageDecay + txRate * (1 - _config.AverageDecay);
        avgRxFast = avgRxFast * _config.AverageDecayFast + rxRate * (1 - _config.AverageDecayFast);
        avgTxFast = avgTxFast * _config.AverageDecayFast + txRate * (1 - _config.AverageDecayFast);
        if (Math.Min(avgRx, avgRxFast) / Math.Max(avgRx, avgRxFast) < 0.5)
            avgRx = avgRxFast = rxRate;
        if (Math.Min(avgTx, avgTxFast) / Math.Max(avgTx, avgTxFast) < 0.5)
            avgTx = avgTxFast = txRate;

        var dto = new TDto { ValidUntilUtc = DateTime.UtcNow + TimeSpan.FromSeconds(10) };

        dto.RxLast = (int)Math.Round(rxRate);
        dto.TxLast = (int)Math.Round(txRate);
        dto.RxAverageRecent = (int)Math.Round(avgRx);
        dto.TxAverageRecent = (int)Math.Round(avgTx);
        var pt5min = _history.Where(p => p.Timestamp > DateTime.UtcNow.AddMinutes(-5)).MinElement(p => p.Timestamp);
        dto.RxAverage5min = (int)Math.Round((pt.RxTotal - pt5min.RxTotal) / (pt.Timestamp - pt5min.Timestamp).TotalSeconds);
        dto.TxAverage5min = (int)Math.Round((pt.TxTotal - pt5min.TxTotal) / (pt.Timestamp - pt5min.Timestamp).TotalSeconds);
        var pt30min = _history.Where(p => p.Timestamp > DateTime.UtcNow.AddMinutes(-30)).MinElement(p => p.Timestamp);
        dto.RxAverage30min = (int)Math.Round((pt.RxTotal - pt30min.RxTotal) / (pt.Timestamp - pt30min.Timestamp).TotalSeconds);
        dto.TxAverage30min = (int)Math.Round((pt.TxTotal - pt30min.TxTotal) / (pt.Timestamp - pt30min.Timestamp).TotalSeconds);
        var pt60min = _history.Where(p => p.Timestamp > DateTime.UtcNow.AddMinutes(-60)).MinElement(p => p.Timestamp);
        dto.RxAverage60min = (int)Math.Round((pt.RxTotal - pt60min.RxTotal) / (pt.Timestamp - pt60min.Timestamp).TotalSeconds);
        dto.TxAverage60min = (int)Math.Round((pt.TxTotal - pt60min.TxTotal) / (pt.Timestamp - pt60min.Timestamp).TotalSeconds);
        dto.HistoryRecent = recentHistory.Select(h => new RouterBlockDto.HistoryPoint { TxRate = h.txRate, RxRate = h.rxRate }).ToArray();
        dto.HistoryHourly = Enumerable.Range(1, 24).Select(h =>
        {
            var from = new DateTime(pt.Timestamp.Year, pt.Timestamp.Month, pt.Timestamp.Day, pt.Timestamp.Hour, 0, 0, DateTimeKind.Utc).AddHours(-24 + h);
            var to = from.AddHours(1);
            var earliest = _history.Where(p => p.Timestamp >= from && p.Timestamp < to).MinElementOrDefault(p => p.Timestamp);
            var latest = _history.Where(p => p.Timestamp >= from && p.Timestamp < to).MaxElementOrDefault(p => p.Timestamp);
            RouterBlockDto.HistoryPoint result = null;
            if (earliest != null && earliest.Timestamp != latest.Timestamp)
                result = new RouterBlockDto.HistoryPoint
                {
                    TxRate = (latest.TxTotal - earliest.TxTotal) / (latest.Timestamp - earliest.Timestamp).TotalSeconds,
                    RxRate = (latest.RxTotal - earliest.RxTotal) / (latest.Timestamp - earliest.Timestamp).TotalSeconds,
                };
            if (result == null || result.TxRate < 0 || result.RxRate < 0)
                return null;
            else
                return result;
        }).ToArray();

        ptPrev = pt;
        return dto;
    }

    private void registerMigrations()
    {
        if (!_db.Enabled)
            return;
        _db.RegisterMigration("RouterService", 0, 1, (conn, trn) =>
        {
            conn.Execute($@"CREATE TABLE {nameof(TbRouterHistoryEntry)} (
                    {nameof(TbRouterHistoryEntry.Timestamp)} INTEGER PRIMARY KEY,
                    {nameof(TbRouterHistoryEntry.TxTotal)} BIGINT NOT NULL,
                    {nameof(TbRouterHistoryEntry.RxTotal)} BIGINT NOT NULL
                )", transaction: trn);
        });
    }

    protected class RouterHistoryPoint
    {
        public DateTime Timestamp;
        public long TxTotal;
        public long RxTotal;
    }

    class TbRouterHistoryEntry
    {
        [ExplicitKey]
        public long Timestamp { get; set; }
        public long TxTotal { get; set; }
        public long RxTotal { get; set; }
    }
}
