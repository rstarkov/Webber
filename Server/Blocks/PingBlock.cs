using System.Net.NetworkInformation;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;
using Webber.Client.Models;

namespace Webber.Server.Blocks;

class PingBlockConfig
{
    public string Host = "8.8.8.8";
    public int IntervalMs = 5000;
    public int MaxWaitMs = 2000;
}

class PingBlockServer : SimpleBlockServerBase<PingBlockDto>
{
    private PingBlockConfig _settings;
    private Queue<(int? ms, DateTime utc)> _recentPings = new();

    public PingBlockServer(IServiceProvider sp, PingBlockConfig settings)
        : base(sp, settings.IntervalMs)
    {
        _settings = settings;
    }

    protected override bool ShouldTick() => true;

    protected override PingBlockDto Tick()
    {
        var ping = new Ping();
        var sentUtc = DateTime.UtcNow;
        var response = ping.Send(_settings.Host, _settings.MaxWaitMs);

        var dto = new PingBlockDto { ValidUntilUtc = sentUtc + TimeSpan.FromSeconds(15) };
        if (response.Status == IPStatus.Success)
            dto.Last = (int) Math.Min(response.RoundtripTime, _settings.MaxWaitMs);
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
