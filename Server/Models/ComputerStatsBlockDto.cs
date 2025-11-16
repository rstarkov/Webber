namespace Webber.Client.Models;

public record ComputerStatsBlockDto : BaseDto
{
    public List<ComputerStats> Computers { get; set; } = new();
}

public record ComputerStats
{
    public string Name { get; set; }
    public List<CpuCoreInfo> CpuCores { get; set; } = new();
    public GpuInfo Gpu { get; set; }

    // Calculated statistics
    public double AvgCpuUtilization { get; set; }
    public double MaxCoreUtilization { get; set; }
    public double MaxGpuUtilization { get; set; }
}

public record CpuCoreInfo
{
    public double Load { get; set; }
    public double Temp { get; set; }
    public int Core { get; set; }
}

public record GpuInfo
{
    public List<GpuLayout> Layout { get; set; } = new();
}

public record GpuLayout
{
    public double Load { get; set; }
    public double Memory { get; set; }
}
