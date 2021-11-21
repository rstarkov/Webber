namespace Webber.Client.Models;

public abstract class BaseDto
{
    public DateTime SentUtc { get; set; }
    public TimeSpan ValidDuration { get; set; }
}
