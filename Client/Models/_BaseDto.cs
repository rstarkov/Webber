namespace Webber.Client.Models;

public abstract record BaseDto
{
    public DateTime SentUtc { get; set; }
    public DateTime ValidUntilUtc { get; init; }
    public string ErrorMessage { get; set; }
}
