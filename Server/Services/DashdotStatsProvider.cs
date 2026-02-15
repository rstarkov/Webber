using Newtonsoft.Json;
using Webber.Client.Models;
using Webber.Server.Blocks;

namespace Webber.Server.Services;

class DashdotStatsProvider : IComputerStatsProvider
{
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromMilliseconds(750) };
    private ulong _totalRam;

    public async Task<ComputerStats> FetchStatsAsync(ComputerConfig config)
    {
        var computerStats = new ComputerStats { Name = config.Name };

        if (string.IsNullOrWhiteSpace(config.MonitoringUrl))
            throw new Exception("No monitoring URL configured");

        var baseUrl = config.MonitoringUrl.TrimEnd('/');

        // Fetch TotalRam on first successful call
        if (_totalRam == 0)
        {
            var infoResponse = await _httpClient.GetStringAsync($"{baseUrl}/info");
            var info = JsonConvert.DeserializeObject<SystemInfoModel>(infoResponse);
            if (info?.Ram?.Size > 0)
                _totalRam = info.Ram.Size;
        }

        // Fetch CPU data - if this fails, the whole method throws and stops
        var cpuResponse = await _httpClient.GetStringAsync($"{baseUrl}/load/cpu");
        var cpuData = JsonConvert.DeserializeObject<List<CpuCoreInfo>>(cpuResponse);
        computerStats.CpuCores = cpuData ?? new List<CpuCoreInfo>();

        // Fetch GPU data
        var gpuResponse = await _httpClient.GetStringAsync($"{baseUrl}/load/gpu");
        var gpuData = JsonConvert.DeserializeObject<GpuInfo>(gpuResponse);
        computerStats.Gpu = gpuData ?? new GpuInfo();

        // Calculate enriched statistics
        if (computerStats.CpuCores.Any())
        {
            computerStats.AvgCpuUtilization = computerStats.CpuCores.Average(c => c.Load);
            computerStats.MaxCoreUtilization = computerStats.CpuCores.Max(c => c.Load);
        }

        if (computerStats.Gpu?.Layout != null && computerStats.Gpu.Layout.Any())
        {
            computerStats.MaxGpuUtilization = computerStats.Gpu.Layout
                .SelectMany(g => new[] { g.Load, g.Memory })
                .Max();
        }

        // Fetch RAM usage and calculate percentage
        if (_totalRam > 0)
        {
            var ramLoadResponse = await _httpClient.GetStringAsync($"{baseUrl}/load/ram");
            var ramLoad = JsonConvert.DeserializeObject<RamLoadInfoModel>(ramLoadResponse);
            if (ramLoad?.Load > 0)
            {
                computerStats.RamUtilization = ((double)ramLoad.Load / _totalRam) * 100;
            }
        }

        return computerStats;
    }

    private class SystemInfoModel
    {
        public RamInfoModel Ram { get; set; }
    }

    private class RamInfoModel
    {
        public ulong Size { get; set; }
    }

    private class RamLoadInfoModel
    {
        public ulong Load { get; set; }
    }
}
