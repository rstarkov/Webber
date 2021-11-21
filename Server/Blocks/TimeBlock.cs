using Webber.Client.Models;

namespace Webber.Server.Blocks;

class TimeBlockConfig
{
    public string LocalTimezoneName = "GMT Standard Time";
    public List<Zone> ExtraTimezones = new List<Zone>();

    public class Zone
    {
        public string DisplayName = null;
        public string TimezoneName = null;
    }
}

class TimeBlockServer : BlockServerBase<TimeBlockDto>
{
    private TimeBlockConfig _config;

    public TimeBlockServer(IServiceProvider sp, TimeBlockConfig config)
        : base(sp)
    {
        _config = config;
    }

    public override void Start()
    {
        new Thread(thread) { IsBackground = true }.Start();
    }

    private void thread()
    {
        while (true)
        {
            try
            {
                var dto = new TimeBlockDto();
                dto.ValidUntilUtc = DateTime.UtcNow + TimeSpan.FromHours(24);
                dto.LocalOffsetHours = getUtcOffset(_config.LocalTimezoneName);
                dto.TimeZones = _config.ExtraTimezones.Select(tz => new TimeBlockDto.TimeZoneDto { DisplayName = tz.DisplayName, OffsetHours = getUtcOffset(tz.TimezoneName) }).ToArray();

                SendUpdate(dto);
            }
            catch
            {
            }

            Thread.Sleep(TimeSpan.FromMinutes(1));
        }
    }

    private double getUtcOffset(string timezoneName)
    {
        return TimeZoneInfo.FindSystemTimeZoneById(timezoneName).GetUtcOffset(DateTimeOffset.UtcNow).TotalHours;
    }

    public override bool MigrateSchema(Microsoft.Data.Sqlite.SqliteConnection db, int curVersion)
    {
        return false;
    }
}
