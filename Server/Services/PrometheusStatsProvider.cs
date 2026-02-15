using Webber.Client.Models;
using Webber.Server.Blocks;

namespace Webber.Server.Services;

class PrometheusStatsProvider : IComputerStatsProvider
{
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromMilliseconds(750) };

    // Previous per-core CPU counters for delta calculation: cpu_index -> (idle, total)
    private Dictionary<int, (double Idle, double Total)> _prevCpuCounters = new();

    public async Task<ComputerStats> FetchStatsAsync(ComputerConfig config)
    {
        var computerStats = new ComputerStats { Name = config.Name };

        if (string.IsNullOrWhiteSpace(config.NodeExporterUrl))
            throw new Exception("No NodeExporterUrl configured");

        var url = config.NodeExporterUrl.TrimEnd('/') + "/metrics";
        var metricsText = await _httpClient.GetStringAsync(url);
        var metrics = ParsePrometheusText(metricsText);

        // --- CPU ---
        var cpuMetrics = metrics.Where(m => m.Name == "node_cpu_seconds_total").ToList();
        var currentCounters = new Dictionary<int, (double Idle, double Total)>();

        foreach (var group in cpuMetrics.GroupBy(m => m.Labels.GetValueOrDefault("cpu", "0")))
        {
            if (!int.TryParse(group.Key, out var cpuIndex))
                continue;

            double total = 0, idle = 0;
            foreach (var m in group)
            {
                if (!double.TryParse(m.Value, out var val))
                    continue;
                total += val;
                if (m.Labels.GetValueOrDefault("mode") == "idle")
                    idle = val;
            }
            currentCounters[cpuIndex] = (idle, total);
        }

        // Compute per-core utilization from delta
        var cpuCores = new List<CpuCoreInfo>();
        foreach (var (cpuIndex, current) in currentCounters.OrderBy(kv => kv.Key))
        {
            double load = 0;
            if (_prevCpuCounters.TryGetValue(cpuIndex, out var prev))
            {
                var totalDelta = current.Total - prev.Total;
                var idleDelta = current.Idle - prev.Idle;
                if (totalDelta > 0)
                    load = (1.0 - idleDelta / totalDelta) * 100.0;
            }
            cpuCores.Add(new CpuCoreInfo { Core = cpuIndex, Load = load, Temp = 0 });
        }

        _prevCpuCounters = currentCounters;
        computerStats.CpuCores = cpuCores;

        // --- RAM ---
        var memTotal = metrics.FirstOrDefault(m => m.Name == "node_memory_MemTotal_bytes");
        var memAvailable = metrics.FirstOrDefault(m => m.Name == "node_memory_MemAvailable_bytes");
        if (memTotal != null && memAvailable != null
            && double.TryParse(memTotal.Value, out var totalBytes)
            && double.TryParse(memAvailable.Value, out var availBytes)
            && totalBytes > 0)
        {
            computerStats.RamUtilization = (totalBytes - availBytes) / totalBytes * 100.0;
        }

        // --- GPU (dcgm-exporter, optional) ---
        computerStats.Gpu = new GpuInfo();
        if (!string.IsNullOrWhiteSpace(config.DcgmExporterUrl))
        {
            try
            {
                var dcgmUrl = config.DcgmExporterUrl.TrimEnd('/') + "/metrics";
                var dcgmText = await _httpClient.GetStringAsync(dcgmUrl);
                var dcgmMetrics = ParsePrometheusText(dcgmText);

                var gpuLoads = dcgmMetrics
                    .Where(m => m.Name == "DCGM_FI_DEV_GPU_UTIL")
                    .ToList();
                var gpuMems = dcgmMetrics
                    .Where(m => m.Name == "DCGM_FI_DEV_MEM_COPY_UTIL")
                    .ToDictionary(m => m.Labels.GetValueOrDefault("gpu", "0"));

                if (gpuLoads.Any())
                {
                    computerStats.Gpu = new GpuInfo
                    {
                        Layout = gpuLoads.Select(g =>
                        {
                            var gpuId = g.Labels.GetValueOrDefault("gpu", "0");
                            double.TryParse(g.Value, out var loadVal);
                            double memVal = 0;
                            if (gpuMems.TryGetValue(gpuId, out var memMetric))
                                double.TryParse(memMetric.Value, out memVal);
                            return new GpuLayout { Load = loadVal, Memory = memVal };
                        }).ToList()
                    };
                }
            }
            catch { }
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

    /// <summary>
    /// Parses Prometheus exposition format text into structured metrics.
    /// Skips comment/type lines (starting with #).
    /// Parses lines like: metric_name{label="val",...} value
    /// </summary>
    private static List<PrometheusMetric> ParsePrometheusText(string text)
    {
        var results = new List<PrometheusMetric>();

        foreach (var line in text.AsSpan().EnumerateLines())
        {
            if (line.IsEmpty || line.IsWhiteSpace() || line.StartsWith("#"))
                continue;

            var lineStr = line.ToString();
            var metric = new PrometheusMetric();

            var braceOpen = lineStr.IndexOf('{');
            if (braceOpen >= 0)
            {
                metric.Name = lineStr.Substring(0, braceOpen);
                var braceClose = lineStr.IndexOf('}', braceOpen);
                if (braceClose < 0) continue;

                // Parse labels
                var labelsStr = lineStr.Substring(braceOpen + 1, braceClose - braceOpen - 1);
                foreach (var pair in SplitLabels(labelsStr))
                {
                    var eq = pair.IndexOf('=');
                    if (eq > 0)
                    {
                        var key = pair.Substring(0, eq);
                        var val = pair.Substring(eq + 1).Trim('"');
                        metric.Labels[key] = val;
                    }
                }

                // Value is everything after the closing brace (and optional space)
                var valueStr = lineStr.Substring(braceClose + 1).Trim();
                // Handle optional timestamp (space-separated after value)
                var spaceIdx = valueStr.IndexOf(' ');
                metric.Value = spaceIdx >= 0 ? valueStr.Substring(0, spaceIdx) : valueStr;
            }
            else
            {
                // No labels: "metric_name value [timestamp]"
                var parts = lineStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) continue;
                metric.Name = parts[0];
                metric.Value = parts[1];
            }

            results.Add(metric);
        }

        return results;
    }

    /// <summary>
    /// Splits label pairs, handling commas inside quoted values.
    /// </summary>
    private static List<string> SplitLabels(string labelsStr)
    {
        var results = new List<string>();
        var current = new System.Text.StringBuilder();
        bool inQuote = false;

        for (int i = 0; i < labelsStr.Length; i++)
        {
            var c = labelsStr[i];
            if (c == '"' && (i == 0 || labelsStr[i - 1] != '\\'))
            {
                inQuote = !inQuote;
                current.Append(c);
            }
            else if (c == ',' && !inQuote)
            {
                var entry = current.ToString().Trim();
                if (entry.Length > 0)
                    results.Add(entry);
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        var last = current.ToString().Trim();
        if (last.Length > 0)
            results.Add(last);

        return results;
    }

    private class PrometheusMetric
    {
        public string Name { get; set; } = "";
        public string Value { get; set; } = "";
        public Dictionary<string, string> Labels { get; set; } = new();
    }
}
