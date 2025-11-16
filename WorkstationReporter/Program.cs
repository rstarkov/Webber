using System.Diagnostics;
using System.Management;
using WorkstationReporter.Models;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var computeCategory = new PerformanceCounterCategory("GPU Engine");
var computeCounterNames = computeCategory.GetInstanceNames();
var computeCounters = new List<PerformanceCounter>();

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
var memoryCountersShared = new List<PerformanceCounter>();

foreach (string counterName in memoryCounterNames)
{
    foreach (var counter in memoryCategory.GetCounters(counterName))
    {
        if (counter.CounterName == "Dedicated Usage")
        {
            memoryCountersDedicated.Add(counter);
        }
        else if (counter.CounterName == "Shared Usage")
        {
            memoryCountersShared.Add(counter);
        }
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

        var sharedMemory = 0f;
        memoryCountersShared.ForEach(x =>
        {
            sharedMemory += x.NextValue();
        });

        var dedicatedMemory = 0f;
        memoryCountersDedicated.ForEach(x =>
        {
            dedicatedMemory += x.NextValue();
        });

        gpuInfo.Layout.Add(new GpuLayout
        {
            Load = gpuLoad,
            Memory = dedicatedMemory
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error querying GPU load: {ex.Message}");
        // Return empty GPU info on error
    }

    return gpuInfo;
});

app.Run();
