namespace Webber.Client.Models;

public record RainCloudBlockDto : BaseDto
{
    public RainCloudPtDto[] Rain { get; set; }
    public RainCloudPtDto[] Cloud { get; set; }
}

public record RainCloudPtDto
{
    public DateTime AtUtc { get; set; } // time instant for which the forecast is computed / the observation is taken
    public int[] Counts { get; set; } // can be null if we have no data
}
