using System.Diagnostics;
using System.Management;
using Hardware.Info;
using WorkstationReporter.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure port from command line or use default
var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 54344;
builder.WebHost.UseUrls($"http://*:{port}");

var app = builder.Build();
Console.WriteLine($"WorkstationReporter listening on port {port}");

var computeCategory = new PerformanceCounterCategory("GPU Engine");
var computeCounterNames = computeCategory.GetInstanceNames();
var computeCounters = new List<PerformanceCounter>();

var hardwareInfo = new HardwareInfo();
hardwareInfo.RefreshVideoControllerList();
hardwareInfo.RefreshMemoryStatus();
var totalDedicatedMemory = hardwareInfo.VideoControllerList.FirstOrDefault()?.AdapterRAM ?? 0;
var totalSystemMemory = hardwareInfo.MemoryStatus.TotalPhysical;

foreach (string counterName in computeCounterNames)
{
    if (counterName.EndsWith("engtype_3D"))
    {
        foreach (PerformanceCounter counter in computeCategory.GetCounters(counterName))
        {
            if (counter.CounterName == "Utilization Percentage")
            {
                computeCounters.Add(counter);
            }
        }
    }
}

var memoryCategory = new PerformanceCounterCategory("GPU Adapter Memory");
var memoryCounterNames = memoryCategory.GetInstanceNames();
var memoryCountersDedicated = new List<PerformanceCounter>();
//var memoryCountersShared = new List<PerformanceCounter>();

foreach (string counterName in memoryCounterNames)
{
    foreach (var counter in memoryCategory.GetCounters(counterName))
    {
        if (counter.CounterName == "Dedicated Usage")
        {
            memoryCountersDedicated.Add(counter);
        }
        //else if (counter.CounterName == "Shared Usage")
        //{
        //    memoryCountersShared.Add(counter);
        //}
    }
}

app.MapGet("/load/cpu", () =>
{
    var cpuCores = new List<CpuCoreInfo>();

    try
    {
        // Query WMI for CPU performance data
        using var searcher = new ManagementObjectSearcher("SELECT Name, PercentProcessorTime FROM Win32_PerfFormattedData_PerfOS_Processor");
        using var results = searcher.Get();

        int coreIndex = 0;
        foreach (ManagementObject obj in results)
        {
            var name = obj["Name"]?.ToString();

            // Skip "_Total" processor (represents all cores combined)
            if (name == "_Total")
                continue;

            var load = Convert.ToDouble(obj["PercentProcessorTime"] ?? 0);

            cpuCores.Add(new CpuCoreInfo
            {
                Core = coreIndex,
                Load = load,
                Temp = 0 // Not reporting temperatures
            });

            coreIndex++;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error querying CPU load: {ex.Message}");
    }

    return cpuCores;
});

app.MapGet("/load/gpu", () =>
{
    var gpuInfo = new GpuInfo();

    try
    {
        var gpuLoad = 0f;
        computeCounters.ForEach(x =>
        {
            gpuLoad += x.NextValue();
        });
        gpuLoad = (float)Math.Round(gpuLoad);

        //var sharedMemory = 0f;
        //memoryCountersShared.ForEach(x =>
        //{
        //    sharedMemory += x.NextValue();
        //});

        var dedicatedMemoryUsed = 0m;
        memoryCountersDedicated.ForEach(x =>
        {
            dedicatedMemoryUsed += (decimal)x.NextValue();
        });

        Console.WriteLine(dedicatedMemoryUsed);

        // Calculate memory usage percentage
        var memoryPercentage = 0.0;
        if (totalDedicatedMemory > 0)
        {
            memoryPercentage = Math.Round((double)((dedicatedMemoryUsed / (decimal)totalDedicatedMemory) * 100));
        }

        gpuInfo.Layout.Add(new GpuLayout
        {
            Load = gpuLoad,
            Memory = memoryPercentage
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error querying GPU load: {ex.Message}");
        // Return empty GPU info on error
    }

    return gpuInfo;
});

app.MapGet("/load/ram", () =>
{
    try
    {
        hardwareInfo.RefreshMemoryStatus();
        var usedMemory = hardwareInfo.MemoryStatus.TotalPhysical - hardwareInfo.MemoryStatus.AvailablePhysical;

        return new RamLoadInfo
        {
            Load = usedMemory
        };
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error querying RAM load: {ex.Message}");
        return new RamLoadInfo { Load = 0 };
    }
});

app.MapGet("/info", () =>
{
    var systemInfo = new SystemInfo
    {
        Ram = new RamInfo
        {
            Size = totalSystemMemory
        }
    };

    return systemInfo;
});

app.Run();
