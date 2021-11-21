namespace Webber.Client.Models;

public abstract record BaseDto
{
    public DateTime SentUtc { get; set; }
    public TimeSpan ValidDuration { get; set; }
    public string ErrorMessage { get; set; }
}
