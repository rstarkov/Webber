using System.Net.NetworkInformation;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;
using Webber.Client.Models;

namespace Webber.Server.Blocks;

class PingBlockConfig
{
    public string Host { get; set; } = "8.8.8.8";
    public int IntervalMs { get; set; } = 5000;
    public int MaxWaitMs { get; set; } = 2000;
}

class PingBlockServer : SimpleBlockServerBase<PingBlockDto>
{
    private PingBlockConfig _config;
    private Queue<(int? ms, DateTime utc)> _recentPings = new();

    public PingBlockServer(IServiceProvider sp, PingBlockConfig config)
        : base(sp, config.IntervalMs)
    {
        _config = config;
    }

    protected override bool ShouldTick() => true;

    protected override PingBlockDto Tick()
    {
        var ping = new Ping();
        var sentUtc = DateTime.UtcNow;
        var response = ping.Send(_config.Host, _config.MaxWaitMs);

        var dto = new PingBlockDto { ValidUntilUtc = sentUtc + TimeSpan.FromSeconds(15) };
        if (response.Status == IPStatus.Success)
            dto.Last = (int) Math.Min(response.RoundtripTime, _config.MaxWaitMs);
        else
            dto.Last = null;

        //using (var db = Db.Open())
        //    db.Insert(new TbPingHistoryEntry { Timestamp = utc.ToDbDateTime(), Ping = dto.Last });

        _recentPings.EnqueueWithMaxCapacity((dto.Last, sentUtc), 24);
        dto.Recent = _recentPings.Select(t => t.ms).ToArray();

        return dto;
    }

    public override bool MigrateSchema(SqliteConnection db, int curVersion)
    {
        if (curVersion == 0)
        {
            if (db == null) return true;
            db.Execute($@"CREATE TABLE {nameof(TbPingHistoryEntry)} (
                    {nameof(TbPingHistoryEntry.Timestamp)} INTEGER PRIMARY KEY,
                    {nameof(TbPingHistoryEntry.Ping)} INT NULL
                )");
            return true;
        }

        return false;
    }
}

class TbPingHistoryEntry
{
    [ExplicitKey]
    public long Timestamp { get; set; }
    public int? Ping { get; set; }
}
