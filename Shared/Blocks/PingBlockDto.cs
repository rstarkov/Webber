namespace Webber.Shared.Blocks;

public class PingBlockDto : BaseDto
{
    public int? Last { get; set; }
    public int?[] Recent { get; set; } = new int?[0];
}
