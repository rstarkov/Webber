using System.Net.NetworkInformation;
using Dapper;
using Dapper.Contrib.Extensions;
using Microsoft.Data.Sqlite;
using Webber.Shared.Blocks;

namespace Webber.Server.Blocks;

class PingBlockConfig
{
    public string Host = "8.8.8.8";
    public int IntervalMs = 5000;
    public int MaxWaitMs = 2000;
}

class PingBlockServer : BlockServerBase<PingBlockDto>
{
    private PingBlockConfig _settings;
    private Queue<(int? ms, DateTime utc)> _recentPings = new();

    public PingBlockServer(IServiceProvider sp, PingBlockConfig settings)
        : base(sp)
    {
        _settings = settings;
    }

    public override void Start()
    {
        new Thread(thread) { IsBackground = true }.Start();
    }

    private void thread()
    {
        while (true)
        {
            var start = DateTime.UtcNow;
            try
            {
                var ping = new Ping();
                var response = ping.Send(_settings.Host, _settings.MaxWaitMs);

                var dto = new PingBlockDto();
                var utc = DateTime.UtcNow;
                dto.ValidUntilUtc = utc + TimeSpan.FromSeconds(15);
                if (response.Status == IPStatus.Success)
                    dto.Last = (int) Math.Min(response.RoundtripTime, _settings.MaxWaitMs);
                else
                    dto.Last = null;

                //using (var db = Db.Open())
                //    db.Insert(new TbPingHistoryEntry { Timestamp = utc.ToDbDateTime(), Ping = dto.Last });

                _recentPings.Enqueue((dto.Last, utc));
                while (_recentPings.Count > 24)
                    _recentPings.Dequeue();
                dto.Recent = _recentPings.Select(t => t.ms).ToArray();

                SendUpdate(dto);
            }
            catch
            {
            }

            Util.SleepUntil(start.AddMilliseconds(_settings.IntervalMs));
        }
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
