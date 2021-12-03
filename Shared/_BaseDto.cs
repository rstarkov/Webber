namespace Webber.Client.Models;

public abstract record BaseDto
{
    public double LocalOffsetHours { get; set; }
    public DateTime SentUtc { get; set; }
    public DateTime ValidUntilUtc { get; init; }
    public string ErrorMessage { get; set; }
}
