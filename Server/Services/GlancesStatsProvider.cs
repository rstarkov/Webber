using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Webber.Client.Models;
using Webber.Server.Blocks;

namespace Webber.Server.Services;

class GlancesStatsProvider : IComputerStatsProvider
{
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromMilliseconds(750) };

    public async Task<ComputerStats> FetchStatsAsync(ComputerConfig config)
    {
        var computerStats = new ComputerStats { Name = config.Name };

        if (string.IsNullOrWhiteSpace(config.MonitoringUrl))
            throw new Exception("No monitoring URL configured");

        var baseUrl = config.MonitoringUrl.TrimEnd('/');

        // Single request to get all plugin data at once (avoids starving the Glances web UI)
        var allResponse = await _httpClient.GetStringAsync($"{baseUrl}/api/4/all");
        var allData = JsonConvert.DeserializeObject<Dictionary<string, JToken>>(allResponse);

        // Parse per-CPU data
        if (allData.TryGetValue("percpu", out var percpuToken))
        {
            var cpuData = percpuToken.ToObject<List<GlancesPerCpuModel>>();
            computerStats.CpuCores = cpuData?.Select(c => new CpuCoreInfo
            {
                Load = c.Total,
                Core = c.CpuNumber,
                Temp = 0
            }).ToList() ?? new List<CpuCoreInfo>();
        }

        // Parse sensor data for CPU core temps
        try
        {
            if (allData.TryGetValue("sensors", out var sensorsToken))
            {
                var sensors = sensorsToken.ToObject<List<GlancesSensorModel>>();
                if (sensors != null)
                {
                    var tempLookup = new Dictionary<int, double>();
                    foreach (var sensor in sensors.Where(s => s.Type == "temperature_core" && s.Label.StartsWith("Core ")))
                    {
                        if (int.TryParse(sensor.Label.Substring(5), out var coreNum))
                        {
                            tempLookup[coreNum] = sensor.Value;
                        }
                    }
                    foreach (var core in computerStats.CpuCores)
                    {
                        if (tempLookup.TryGetValue(core.Core, out var temp))
                        {
                            core.Temp = temp;
                        }
                    }
                }
            }
        }
        catch { }

        // Parse GPU data
        computerStats.Gpu = new GpuInfo();
        try
        {
            if (allData.TryGetValue("gpu", out var gpuToken))
            {
                var gpuData = gpuToken.ToObject<List<GlancesGpuModel>>();
                if (gpuData != null && gpuData.Any())
                {
                    computerStats.Gpu = new GpuInfo
                    {
                        Layout = gpuData.Select(g => new GpuLayout
                        {
                            Load = g.Proc,
                            Memory = g.Mem
                        }).ToList()
                    };
                }
            }
        }
        catch { }

        // Parse RAM data
        if (allData.TryGetValue("mem", out var memToken))
        {
            var memData = memToken.ToObject<GlancesMemModel>();
            if (memData != null)
            {
                computerStats.RamUtilization = memData.Percent;
            }
        }

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

        return computerStats;
    }

    private class GlancesPerCpuModel
    {
        [JsonProperty("cpu_number")]
        public int CpuNumber { get; set; }

        [JsonProperty("total")]
        public double Total { get; set; }
    }

    private class GlancesGpuModel
    {
        [JsonProperty("gpu_id")]
        public string GpuId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("mem")]
        public double Mem { get; set; }

        [JsonProperty("proc")]
        public double Proc { get; set; }

        [JsonProperty("temperature")]
        public double Temperature { get; set; }
    }

    private class GlancesMemModel
    {
        [JsonProperty("total")]
        public ulong Total { get; set; }

        [JsonProperty("used")]
        public ulong Used { get; set; }

        [JsonProperty("percent")]
        public double Percent { get; set; }
    }

    private class GlancesSensorModel
    {
        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("unit")]
        public string Unit { get; set; }

        [JsonProperty("value")]
        public double Value { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }
    }
}
