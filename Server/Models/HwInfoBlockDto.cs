namespace Webber.Client.Models;

public readonly record struct TimedMetric(DateTime TimeUtc, double Value);

public record HwInfoBlockDto : BaseDto
{
    public double MemoryUtiliZation { get; set; }
    public double[] CpuCoreHeatmap { get; set; } = new double[0];
    public double CpuTotalLoad { get; set; }
    public TimedMetric[] CpuTotalLoadHistory { get; set; } = new TimedMetric[0];
    public double CpuPackageTemp { get; set; }
    public TimedMetric[] CpuPackageTempHistory { get; set; } = new TimedMetric[0];
    public double GpuLoad { get; set; }
    public TimedMetric[] GpuLoadHistory { get; set; } = new TimedMetric[0];
    public double GpuTemp { get; set; }
    public TimedMetric[] GpuTempHistory { get; set; } = new TimedMetric[0];
    public double NetworkUp { get; set; }
    public TimedMetric[] NetworkUpHistory { get; set; } = new TimedMetric[0];
    public double NetworkDown { get; set; }
    public TimedMetric[] NetworkDownHistory { get; set; } = new TimedMetric[0];
    public double NetworkPing { get; set; }
    public TimedMetric[] NetworkPingHistory { get; set; } = new TimedMetric[0];
}
