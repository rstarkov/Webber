using Webber.Client.Models;

namespace Webber.Server.Blocks;

class TimeBlockConfig
{
    public string LocalTimezoneName { get; set; } = "GMT Standard Time";
    public List<Zone> ExtraTimezones { get; set; } = new List<Zone>();

    public class Zone
    {
        public string DisplayName { get; set; }
        public string TimezoneName { get; set; }
    }
}

class TimeBlockServer : SimpleBlockServerBase<TimeBlockDto>
{
    private TimeBlockConfig _config;

    public TimeBlockServer(IServiceProvider sp, TimeBlockConfig config)
        : base(sp, TimeSpan.FromMinutes(1))
    {
        _config = config;
    }

    protected override TimeBlockDto Tick()
    {
        var dto = new TimeBlockDto { ValidUntilUtc = DateTime.UtcNow + TimeSpan.FromHours(24) };
        dto.LocalOffsetHours = getUtcOffset(_config.LocalTimezoneName);
        dto.TimeZones = _config.ExtraTimezones.Select(tz => new TimeBlockDto.TimeZoneDto { DisplayName = tz.DisplayName, OffsetHours = getUtcOffset(tz.TimezoneName) }).ToArray();
        return dto;
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
