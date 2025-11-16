using Newtonsoft.Json;
using Webber.Client.Models;

namespace Webber.Server.Blocks;

class ComputerStatsBlockConfig
{
    public List<ComputerConfig> Computers { get; set; } = new();
    public int IntervalMs { get; set; } = 5000; // Default: poll every 5 seconds
}

class ComputerConfig
{
    public string Name { get; set; }
    public string CpuUrl { get; set; }
    public string GpuUrl { get; set; }
}

class ComputerStatsBlockServer : SimpleBlockServerBase<ComputerStatsBlockDto>
{
    private ComputerStatsBlockConfig _config;
    private HttpClient _httpClient = new();

    public ComputerStatsBlockServer(IServiceProvider sp, ComputerStatsBlockConfig config)
        : base(sp, config.IntervalMs)
    {
        _config = config;
    }

    protected override ComputerStatsBlockDto Tick()
    {
        var computers = new List<ComputerStats>();

        foreach (var computerConfig in _config.Computers)
        {
            try
            {
                var computerStats = new ComputerStats
                {
                    Name = computerConfig.Name
                };

                // Fetch CPU data
                if (!string.IsNullOrWhiteSpace(computerConfig.CpuUrl))
                {
                    var cpuResponse = _httpClient.GetStringAsync(computerConfig.CpuUrl)
                        .GetAwaiter().GetResult();
                    var cpuData = JsonConvert.DeserializeObject<List<CpuCoreInfo>>(cpuResponse);
                    computerStats.CpuCores = cpuData ?? new List<CpuCoreInfo>();
                }

                // Fetch GPU data
                if (!string.IsNullOrWhiteSpace(computerConfig.GpuUrl))
                {
                    var gpuResponse = _httpClient.GetStringAsync(computerConfig.GpuUrl)
                        .GetAwaiter().GetResult();
                    var gpuData = JsonConvert.DeserializeObject<GpuInfo>(gpuResponse);
                    computerStats.Gpu = gpuData ?? new GpuInfo();
                }

                // Calculate enriched statistics
                if (computerStats.CpuCores.Any())
                {
                    computerStats.AvgCpuUtilization = computerStats.CpuCores.Average(c => c.Load);
                    computerStats.MaxCoreUtilization = computerStats.CpuCores.Max(c => c.Load);
                }

                if (computerStats.Gpu?.Layout != null && computerStats.Gpu.Layout.Any())
                {
                    // Max GPU utilization is the maximum of load and memory across all layout items
                    computerStats.MaxGpuUtilization = computerStats.Gpu.Layout
                        .SelectMany(g => new[] { g.Load, g.Memory })
                        .Max();
                }

                computers.Add(computerStats);
            }
            catch (Exception ex)
            {
                // Log error but continue processing other computers
                Logger.LogWarning($"Failed to fetch stats for computer '{computerConfig.Name}': {ex.Message}");

                // Add computer with empty data to indicate it's configured but unavailable
                computers.Add(new ComputerStats
                {
                    Name = computerConfig.Name,
                    CpuCores = new List<CpuCoreInfo>(),
                    Gpu = new GpuInfo()
                });
            }
        }

        return new ComputerStatsBlockDto
        {
            Computers = computers,
            ValidUntilUtc = DateTime.UtcNow.AddSeconds(_config.IntervalMs / 1000 * 2) // Valid for 2 intervals
        };
    }
}
