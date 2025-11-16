using System.Diagnostics;
using Hardware.Info;
using WorkstationReporter.Models;

namespace WorkstationReporter.Services;

public class StatsCollectorService : BackgroundService
{
    private readonly ILogger<StatsCollectorService> _logger;
    private List<PerformanceCounter> _computeCounters = new();
    private List<PerformanceCounter> _memoryCountersDedicated = new();
    private readonly HardwareInfo _hardwareInfo;
    private readonly ulong _totalDedicatedMemory;
    private readonly ulong _totalSystemMemory;

    private readonly SemaphoreSlim _wakeSignal = new(0, 1);
    private DateTime _lastRequestTime = DateTime.MinValue;
    private readonly TimeSpan _sleepTimeout = TimeSpan.FromSeconds(30);
    private readonly object _lock = new();
    private DateTime _lastCounterRefresh = DateTime.UtcNow;
    private readonly TimeSpan _counterRefreshInterval = TimeSpan.FromSeconds(15);

    // Cached values
    private List<CpuCoreInfo> _cachedCpuCores = new();
    private GpuInfo _cachedGpuInfo = new();
    private RamLoadInfo _cachedRamLoadInfo = new();

    public StatsCollectorService(ILogger<StatsCollectorService> logger)
    {
        _logger = logger;

        // Initialize hardware info
        _hardwareInfo = new HardwareInfo();
        _hardwareInfo.RefreshVideoControllerList();
        _hardwareInfo.RefreshMemoryStatus();
        _totalDedicatedMemory = _hardwareInfo.VideoControllerList.FirstOrDefault()?.AdapterRAM ?? 0;
        _totalSystemMemory = _hardwareInfo.MemoryStatus.TotalPhysical;

        // Initialize GPU performance counters
        RefreshGpuCounters();
    }

    private void RefreshGpuCounters()
    {
        lock (_lock)
        {
            // Dispose old counters
            _computeCounters.ForEach(c => c.Dispose());
            _memoryCountersDedicated.ForEach(c => c.Dispose());
            _computeCounters.Clear();
            _memoryCountersDedicated.Clear();

            // Reinitialize GPU performance counters
            var computeCategory = new PerformanceCounterCategory("GPU Engine");
            var computeCounterNames = computeCategory.GetInstanceNames();

            foreach (string counterName in computeCounterNames)
            {
                if (counterName.EndsWith("engtype_3D"))
                {
                    foreach (PerformanceCounter counter in computeCategory.GetCounters(counterName))
                    {
                        if (counter.CounterName == "Utilization Percentage")
                        {
                            _computeCounters.Add(counter);
                        }
                    }
                }
            }

            var memoryCategory = new PerformanceCounterCategory("GPU Adapter Memory");
            var memoryCounterNames = memoryCategory.GetInstanceNames();

            foreach (string counterName in memoryCounterNames)
            {
                foreach (var counter in memoryCategory.GetCounters(counterName))
                {
                    if (counter.CounterName == "Dedicated Usage")
                    {
                        _memoryCountersDedicated.Add(counter);
                    }
                }
            }

            _lastCounterRefresh = DateTime.UtcNow;
            _logger.LogInformation("Refreshed GPU counters: {ComputeCounters} compute, {MemoryCounters} memory",
                _computeCounters.Count, _memoryCountersDedicated.Count);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("StatsCollectorService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            // Check if we should sleep
            TimeSpan timeSinceLastRequest;
            lock (_lock)
            {
                timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
            }

            if (_lastRequestTime != DateTime.MinValue && timeSinceLastRequest > _sleepTimeout)
            {
                _logger.LogInformation("No requests for {Timeout}s, putting thread to sleep", _sleepTimeout.TotalSeconds);

                // Wait for wake signal or cancellation
                try
                {
                    await _wakeSignal.WaitAsync(stoppingToken);
                    _logger.LogInformation("Thread woken up by request");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }

            // Check if we need to refresh GPU counters
            if (DateTime.UtcNow - _lastCounterRefresh > _counterRefreshInterval)
            {
                RefreshGpuCounters();
            }

            // Update all stats
            UpdateCpuStats();
            UpdateGpuStats();
            UpdateRamStats();

            // Wait 1 second before next update
            try
            {
                await Task.Delay(500, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("StatsCollectorService stopped");
    }

    private void UpdateCpuStats()
    {
        var cpuCores = new List<CpuCoreInfo>();

        try
        {
            _hardwareInfo.RefreshCPUList();

            int coreIndex = 0;
            foreach (var cpu in _hardwareInfo.CpuList)
            {
                foreach (var core in cpu.CpuCoreList)
                {
                    var load = (double)core.PercentProcessorTime;

                    // PercentProcessorTime should be 0-100, but verify it's reasonable
                    if (load > 100)
                    {
                        _logger.LogWarning("Unexpected CPU load value: {Load} for core {Core}", load, coreIndex);
                        load = Math.Min(load, 100);
                    }

                    cpuCores.Add(new CpuCoreInfo
                    {
                        Core = coreIndex,
                        Load = load,
                        Temp = 0
                    });

                    coreIndex++;
                }
            }

            lock (_lock)
            {
                _cachedCpuCores = cpuCores;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating CPU stats");
        }
    }

    private void UpdateGpuStats()
    {
        var gpuInfo = new GpuInfo();
        var needsRefresh = false;

        try
        {
            var gpuLoad = 0f;
            foreach (var counter in _computeCounters.ToList())
            {
                try
                {
                    gpuLoad += counter.NextValue();
                }
                catch (InvalidOperationException)
                {
                    // Counter instance no longer exists (process terminated), trigger refresh
                    needsRefresh = true;
                }
            }
            gpuLoad = (float)Math.Round(gpuLoad);

            var dedicatedMemoryUsed = 0m;
            foreach (var counter in _memoryCountersDedicated.ToList())
            {
                try
                {
                    dedicatedMemoryUsed += (decimal)counter.NextValue();
                }
                catch (InvalidOperationException)
                {
                    // Counter instance no longer exists (process terminated), trigger refresh
                    needsRefresh = true;
                }
            }

            var memoryPercentage = 0.0;
            if (_totalDedicatedMemory > 0)
            {
                memoryPercentage = Math.Round((double)((dedicatedMemoryUsed / (decimal)_totalDedicatedMemory) * 100));
            }

            gpuInfo.Layout.Add(new GpuLayout
            {
                Load = gpuLoad,
                Memory = memoryPercentage
            });

            lock (_lock)
            {
                _cachedGpuInfo = gpuInfo;
            }

            // Refresh counters if we hit stale instances
            if (needsRefresh)
            {
                _logger.LogInformation("Detected stale GPU counters, triggering refresh");
                RefreshGpuCounters();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating GPU stats");
        }
    }

    private void UpdateRamStats()
    {
        try
        {
            _hardwareInfo.RefreshMemoryStatus();
            var usedMemory = _hardwareInfo.MemoryStatus.TotalPhysical - _hardwareInfo.MemoryStatus.AvailablePhysical;

            lock (_lock)
            {
                _cachedRamLoadInfo = new RamLoadInfo
                {
                    Load = usedMemory
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating RAM stats");
        }
    }

    public List<CpuCoreInfo> GetCpuStats()
    {
        NotifyRequest();
        lock (_lock)
        {
            return _cachedCpuCores;
        }
    }

    public GpuInfo GetGpuStats()
    {
        NotifyRequest();
        lock (_lock)
        {
            return _cachedGpuInfo;
        }
    }

    public RamLoadInfo GetRamStats()
    {
        NotifyRequest();
        lock (_lock)
        {
            return _cachedRamLoadInfo;
        }
    }

    public SystemInfo GetSystemInfo()
    {
        return new SystemInfo
        {
            Ram = new RamInfo
            {
                Size = _totalSystemMemory
            }
        };
    }

    private void NotifyRequest()
    {
        lock (_lock)
        {
            _lastRequestTime = DateTime.UtcNow;
        }

        // Try to wake up the thread if it's sleeping
        if (_wakeSignal.CurrentCount == 0)
        {
            try
            {
                _wakeSignal.Release();
            }
            catch (SemaphoreFullException)
            {
                // Already awake, ignore
            }
        }
    }

    public override void Dispose()
    {
        _wakeSignal?.Dispose();
        _computeCounters.ForEach(c => c.Dispose());
        _memoryCountersDedicated.ForEach(c => c.Dispose());
        base.Dispose();
    }
}
