namespace Webber.Client.Models;

public record ReloadBlockDto : BaseDto
{
    public string ServerHash { get; set; }
}
