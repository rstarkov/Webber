namespace Webber.Client.Models;

public record TimedMetric(DateTime TimeUtc, double Value);

public class HwInfoBlockDto : BaseDto
{
    public double[] CpuCoreHeatmap { get; set; } = new double[0];
    public TimedMetric[] CpuTotalLoadHistory { get; set; } = new TimedMetric[0];
    public TimedMetric[] CpuPackageTempHistory { get; set; } = new TimedMetric[0];
}
