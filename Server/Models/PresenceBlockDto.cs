namespace Webber.Client.Models;

public record PresenceBlockDto : BaseDto
{
    public bool PresenceDetected { get; set; }
    public bool SessionUnlocked { get; set; }
}
