namespace Webber.Client.Models;

public class TimeBlockDto : BaseDto
{
    public double LocalOffsetHours { get; set; }
    public TimeZoneDto[] TimeZones { get; set; }

    public class TimeZoneDto
    {
        public string DisplayName { get; set; }
        public double OffsetHours { get; set; }
    }
}
