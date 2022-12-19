namespace Webber.Client.Models;


public record HwInfoBlockDto : BaseDto
{
    public double MemoryUtilization { get; set; }
    public double CpuTotalLoad { get; set; }
    public double CpuMaxCoreLoad { get; set; }
    public string CpuMaxCoreName { get; set; }
    public double GpuLoad { get; set; }
}
