namespace WorkstationReporter.Models;

public record CpuCoreInfo
{
    public double Load { get; set; }
    public double Temp { get; set; }
    public int Core { get; set; }
}

public record GpuLayout
{
    public double Load { get; set; }
    public double Memory { get; set; }
}

public record GpuInfo
{
    public List<GpuLayout> Layout { get; set; } = new();
}

public record RamInfo
{
    public ulong Size { get; set; }
}

public record SystemInfo
{
    public RamInfo Ram { get; set; } = new();
}

public record RamLoadInfo
{
    public ulong Load { get; set; }
}
