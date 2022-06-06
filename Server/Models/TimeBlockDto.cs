namespace Webber.Client.Models;

public record TimeBlockDto : BaseDto
{
    public TimeZoneDto[] TimeZones { get; set; }

    public record TimeZoneDto
    {
        public string DisplayName { get; set; }
        public double OffsetHours { get; set; }
    }
}
