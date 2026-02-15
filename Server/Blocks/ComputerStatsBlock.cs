using System.Net;
using Lextm.SharpSnmpLib;
using Lextm.SharpSnmpLib.Messaging;
using Lextm.SharpSnmpLib.Security;
using Newtonsoft.Json;
using Webber.Client.Models;

namespace Webber.Server.Blocks;

// Models for WorkstationReporter API responses
class SystemInfo
{
    public RamInfo Ram { get; set; }
}

class RamInfo
{
    public ulong Size { get; set; }
}

class RamLoadInfo
{
    public ulong Load { get; set; }
}

// Models for Glances API responses
class GlancesPerCpu
{
    [JsonProperty("cpu_number")]
    public int CpuNumber { get; set; }

    [JsonProperty("total")]
    public double Total { get; set; }
}

class GlancesGpu
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

class GlancesMem
{
    [JsonProperty("total")]
    public ulong Total { get; set; }

    [JsonProperty("used")]
    public ulong Used { get; set; }

    [JsonProperty("percent")]
    public double Percent { get; set; }
}

class GlancesSensor
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

class ComputerStatsBlockConfig
{
    public List<ComputerConfig> Computers { get; set; } = new();
    public int IntervalMs { get; set; } = 5000; // Default: poll every 5 seconds
}

class ComputerConfig
{
    public string Name { get; set; }

    // Base URL for monitoring endpoints (e.g., "http://192.168.1.6:3001")
    // Will append /load/cpu, /load/gpu, /load/ram, /info as needed
    public string MonitoringUrl { get; set; }

    // API backend: "dashdot" (default) or "glances"
    public string ApiMode { get; set; } = "dashdot";

    // Optional SNMP settings for power consumption from UPS
    public string SnmpHost { get; set; }
    public string SnmpUser { get; set; }
    public string SnmpPassword { get; set; }
    public string SnmpOid { get; set; } = "1.3.6.1.4.1.534.1.4.4.1.4.1"; // Default OID for Eaton UPS output watts
}

class ComputerInfo
{
    public int ConsecutiveFailures { get; set; }
    public bool IsOffline { get; set; }
    public int TicksSinceLastCheck { get; set; }
    public ulong TotalRam { get; set; }
}

class ComputerStatsBlockServer : SimpleBlockServerBase<ComputerStatsBlockDto>
{
    private ComputerStatsBlockConfig _config;
    private HttpClient _httpClient = new();
    private Dictionary<string, SnmpConnection> _snmpConnections = new();
    private Dictionary<string, ComputerInfo> _computerInfo = new();
    private Dictionary<string, ComputerStats> _lastSuccessfulResults = new();

    public ComputerStatsBlockServer(IServiceProvider sp, ComputerStatsBlockConfig config)
        : base(sp, config.IntervalMs)
    {
        _config = config;

        _httpClient.Timeout = TimeSpan.FromMilliseconds(750);

        // Initialize SNMP connections and computer info for each computer
        foreach (var computer in config.Computers)
        {
            // Initialize computer info
            _computerInfo[computer.Name] = new ComputerInfo
            {
                ConsecutiveFailures = 0,
                IsOffline = false,
                TicksSinceLastCheck = 0,
                TotalRam = 0
            };

            if (!string.IsNullOrWhiteSpace(computer.SnmpHost))
            {
                try
                {
                    _snmpConnections[computer.Name] = InitializeSnmpConnection(computer);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Failed to initialize SNMP for '{computer.Name}': {ex.Message}");
                }
            }

            // Fetch and cache total RAM at startup
            if (!string.IsNullOrWhiteSpace(computer.MonitoringUrl))
            {
                try
                {
                    var baseUrl = computer.MonitoringUrl.TrimEnd('/');
                    if (computer.ApiMode == "glances")
                    {
                        var memResponse = _httpClient.GetStringAsync($"{baseUrl}/api/4/mem").GetAwaiter().GetResult();
                        var mem = JsonConvert.DeserializeObject<GlancesMem>(memResponse);
                        if (mem?.Total > 0)
                        {
                            _computerInfo[computer.Name].TotalRam = mem.Total;
                            Logger.LogInformation($"Cached total RAM for '{computer.Name}' (glances): {mem.Total / (1024UL * 1024 * 1024)} GB");
                        }
                    }
                    else
                    {
                        var infoResponse = _httpClient.GetStringAsync($"{baseUrl}/info").GetAwaiter().GetResult();
                        var info = JsonConvert.DeserializeObject<SystemInfo>(infoResponse);
                        if (info?.Ram?.Size > 0)
                        {
                            _computerInfo[computer.Name].TotalRam = info.Ram.Size;
                            Logger.LogInformation($"Cached total RAM for '{computer.Name}': {info.Ram.Size / (1024 * 1024 * 1024)} GB");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Failed to fetch initial info for '{computer.Name}': {ex.Message}");
                    // Mark as offline immediately if initial fetch fails
                    _computerInfo[computer.Name].IsOffline = true;
                    _computerInfo[computer.Name].ConsecutiveFailures = 1;
                }
            }
        }
    }

    private class SnmpConnection
    {
        public IPEndPoint Endpoint { get; set; }
        public OctetString UserName { get; set; }
        public IPrivacyProvider Privacy { get; set; }
        public ObjectIdentifier Oid { get; set; }
    }

    private SnmpConnection InitializeSnmpConnection(ComputerConfig config)
    {
        // Parse host and port
        var hostParts = config.SnmpHost.Split(':');
        var host = hostParts[0];
        var port = hostParts.Length > 1 ? int.Parse(hostParts[1]) : 161;

        var endpoint = new IPEndPoint(IPAddress.Parse(host), port);
        var userName = new OctetString(config.SnmpUser);

        // Create authentication provider (SHA-1, No Privacy)
        // Note: SHA-1 is required for compatibility with Eaton UPS
#pragma warning disable CS0618
        var auth = new SHA1AuthenticationProvider(new OctetString(config.SnmpPassword));
#pragma warning restore CS0618
        var privacy = new DefaultPrivacyProvider(auth);

        return new SnmpConnection
        {
            Endpoint = endpoint,
            UserName = userName,
            Privacy = privacy,
            Oid = new ObjectIdentifier(config.SnmpOid)
        };
    }

    private async Task<ComputerStats> FetchComputerStatsAsync(ComputerConfig config)
    {
        var computerStats = new ComputerStats { Name = config.Name };

        if (string.IsNullOrWhiteSpace(config.MonitoringUrl))
        {
            throw new Exception("No monitoring URL configured");
        }

        var baseUrl = config.MonitoringUrl.TrimEnd('/');

        // Fetch CPU data - if this fails, the whole method throws and stops
        var cpuUrl = $"{baseUrl}/load/cpu";
        var cpuResponse = await _httpClient.GetStringAsync(cpuUrl);
        var cpuData = JsonConvert.DeserializeObject<List<CpuCoreInfo>>(cpuResponse);
        computerStats.CpuCores = cpuData ?? new List<CpuCoreInfo>();

        // Fetch GPU data
        var gpuUrl = $"{baseUrl}/load/gpu";
        var gpuResponse = await _httpClient.GetStringAsync(gpuUrl);
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
        var info = _computerInfo[config.Name];
        if (info.TotalRam > 0)
        {
            var ramLoadUrl = $"{baseUrl}/load/ram";
            var ramLoadResponse = await _httpClient.GetStringAsync(ramLoadUrl);
            var ramLoad = JsonConvert.DeserializeObject<RamLoadInfo>(ramLoadResponse);

            if (ramLoad?.Load > 0 && info.TotalRam > 0)
            {
                computerStats.RamUtilization = ((double)ramLoad.Load / info.TotalRam) * 100;
            }
        }

        return computerStats;
    }

    private async Task<ComputerStats> FetchGlancesStatsAsync(ComputerConfig config)
    {
        var computerStats = new ComputerStats { Name = config.Name };

        if (string.IsNullOrWhiteSpace(config.MonitoringUrl))
        {
            throw new Exception("No monitoring URL configured");
        }

        var baseUrl = config.MonitoringUrl.TrimEnd('/');

        // Single request to get all plugin data at once (avoids starving the Glances web UI)
        var allResponse = await _httpClient.GetStringAsync($"{baseUrl}/api/4/all");
        var allData = JsonConvert.DeserializeObject<Dictionary<string, Newtonsoft.Json.Linq.JToken>>(allResponse);

        // Parse per-CPU data
        if (allData.TryGetValue("percpu", out var percpuToken))
        {
            var cpuData = percpuToken.ToObject<List<GlancesPerCpu>>();
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
                var sensors = sensorsToken.ToObject<List<GlancesSensor>>();
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
                var gpuData = gpuToken.ToObject<List<GlancesGpu>>();
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
            var memData = memToken.ToObject<GlancesMem>();
            if (memData != null)
            {
                computerStats.RamUtilization = memData.Percent;
                if (memData.Total > 0)
                {
                    _computerInfo[config.Name].TotalRam = memData.Total;
                }
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

    private async Task<double?> FetchPowerConsumptionAsync(string computerName)
    {
        if (!_snmpConnections.TryGetValue(computerName, out var snmpConn))
        {
            return null;
        }

        return await Task.Run(() => GetPowerConsumptionSnmp(snmpConn));
    }

    private async Task<bool> CheckComputerOnlineAsync(ComputerConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.MonitoringUrl))
        {
            return false;
        }

        var baseUrl = config.MonitoringUrl.TrimEnd('/');

        if (config.ApiMode == "glances")
        {
            // Check Glances health endpoint
            var statusResponse = await _httpClient.GetAsync($"{baseUrl}/api/4/status");
            if (!statusResponse.IsSuccessStatusCode)
                return false;

            // Refresh TotalRam from /mem
            var memResponse = await _httpClient.GetStringAsync($"{baseUrl}/api/4/mem");
            var mem = JsonConvert.DeserializeObject<GlancesMem>(memResponse);
            if (mem?.Total > 0)
            {
                _computerInfo[config.Name].TotalRam = mem.Total;
                return true;
            }

            return false;
        }
        else
        {
            var infoResponse = await _httpClient.GetStringAsync($"{baseUrl}/info");
            var systemInfo = JsonConvert.DeserializeObject<SystemInfo>(infoResponse);

            if (systemInfo?.Ram?.Size > 0)
            {
                _computerInfo[config.Name].TotalRam = systemInfo.Ram.Size;
                return true;
            }

            return false;
        }
    }

    private ComputerStats CreateOfflineStats(string name)
    {
        return new ComputerStats
        {
            Name = name,
            IsOffline = true,
            CpuCores = new List<CpuCoreInfo>(),
            Gpu = new GpuInfo()
        };
    }

    private Task<ComputerStats> HandleOfflineComputerAsync(ComputerConfig config)
    {
        var info = _computerInfo[config.Name];
        info.TicksSinceLastCheck++;

        // Only check /info every 10 ticks when offline
        if (info.TicksSinceLastCheck < 10)
        {
            return Task.FromResult(CreateOfflineStats(config.Name));
        }

        info.TicksSinceLastCheck = 0;

        // Try to bring computer back online
        return CheckComputerOnlineAsync(config).ContinueWith(task =>
        {
            if (task.IsCompletedSuccessfully && task.Result)
            {
                info.IsOffline = false;
                info.ConsecutiveFailures = 0;
                Logger.LogInformation($"Computer '{config.Name}' is back online. Updated total RAM: {info.TotalRam / (1024 * 1024 * 1024)} GB");
                return config.ApiMode == "glances"
                    ? FetchGlancesStatsAsync(config)
                    : FetchComputerStatsAsync(config);
            }

            Logger.LogDebug($"Computer '{config.Name}' still offline");
            return Task.FromResult(CreateOfflineStats(config.Name));
        }).Unwrap();
    }

    private void ProcessComputerResult(ComputerConfig config, Task<ComputerStats> task, Task<double?> powerTask, List<ComputerStats> computers)
    {
        var info = _computerInfo[config.Name];

        if (task.IsCompletedSuccessfully)
        {
            var stats = task.Result;

            // If this is offline stats (returned from HandleOfflineComputerAsync), add power and return
            if (stats.IsOffline)
            {
                // Add power consumption if available (UPS can report power even when computer is offline)
                if (powerTask.IsCompletedSuccessfully)
                {
                    stats.PowerConsumptionWatts = powerTask.Result;
                }

                computers.Add(stats);
                return;
            }

            // This is real data from a successful fetch
            info.ConsecutiveFailures = 0;
            stats.IsOffline = false;

            // Add power consumption if available
            if (powerTask.IsCompletedSuccessfully)
            {
                stats.PowerConsumptionWatts = powerTask.Result;
            }
            else if (_snmpConnections.ContainsKey(config.Name))
            {
                Logger.LogWarning($"Failed to fetch power consumption for '{config.Name}': {powerTask.Exception?.InnerException?.Message ?? powerTask.Exception?.Message}");
            }

            // Cache successful result
            _lastSuccessfulResults[config.Name] = stats;
            computers.Add(stats);
            return;
        }

        // Failed
        info.ConsecutiveFailures++;
        Logger.LogWarning($"Failed to fetch stats for computer '{config.Name}' (failure {info.ConsecutiveFailures}/5): {task.Exception?.InnerException?.Message ?? task.Exception?.Message}");

        if (info.ConsecutiveFailures >= 5 && !info.IsOffline)
        {
            info.IsOffline = true;
            info.TicksSinceLastCheck = 0;
            Logger.LogWarning($"Computer '{config.Name}' marked as OFFLINE after 5 consecutive failures");
            // Clear cache when going offline
            _lastSuccessfulResults.Remove(config.Name);
        }

        // Return cached result if available and not offline, otherwise return offline stats
        if (!info.IsOffline && _lastSuccessfulResults.TryGetValue(config.Name, out var cachedStats))
        {
            Logger.LogInformation($"Using cached stats for '{config.Name}' due to temporary failure");
            computers.Add(cachedStats);
        }
        else
        {
            computers.Add(CreateOfflineStats(config.Name));
        }
    }

    protected override ComputerStatsBlockDto Tick()
    {
        var computerTasks = new List<Task<ComputerStats>>();
        var powerTasks = new List<Task<double?>>();
        var computerConfigs = new List<ComputerConfig>();

        // Prepare tasks for all computers
        foreach (var computerConfig in _config.Computers)
        {
            var info = _computerInfo[computerConfig.Name];

            // Create computer stats task
            if (info.IsOffline)
            {
                computerTasks.Add(HandleOfflineComputerAsync(computerConfig));
            }
            else
            {
                computerTasks.Add(computerConfig.ApiMode == "glances"
                    ? FetchGlancesStatsAsync(computerConfig)
                    : FetchComputerStatsAsync(computerConfig));
            }

            // Create power consumption task
            powerTasks.Add(_snmpConnections.ContainsKey(computerConfig.Name)
                ? FetchPowerConsumptionAsync(computerConfig.Name)
                : Task.FromResult<double?>(null));

            computerConfigs.Add(computerConfig);
        }

        // Wait for all tasks to complete (ignore exceptions, we handle them per-task below)
        try
        {
            Task.WaitAll(computerTasks.Cast<Task>().Concat(powerTasks).ToArray());
        }
        catch (AggregateException)
        {
            // Some tasks failed, we'll handle them individually below
        }

        // Process results
        var computers = new List<ComputerStats>();
        for (int i = 0; i < computerTasks.Count; i++)
        {
            ProcessComputerResult(computerConfigs[i], computerTasks[i], powerTasks[i], computers);
        }

        return new ComputerStatsBlockDto
        {
            Computers = computers,
            ValidUntilUtc = DateTime.UtcNow.AddSeconds(_config.IntervalMs / 1000 * 2)
        };
    }

    private double? GetPowerConsumptionSnmp(SnmpConnection conn)
    {
        try
        {
            // Redo discovery each time to get fresh engine boots/time
            var discovery = Messenger.GetNextDiscovery(SnmpType.GetRequestPdu);
            var report = discovery.GetResponse(1000, conn.Endpoint);

            var variables = new List<Variable> { new Variable(conn.Oid) };

#pragma warning disable CS0618
            var request = new GetRequestMessage(
                VersionCode.V3,
                Messenger.NextMessageId,
                Messenger.NextRequestId,
                conn.UserName,
                variables,
                conn.Privacy,
                Messenger.MaxMessageSize,
                report
            );
#pragma warning restore CS0618

            var reply = request.GetResponse(1000, conn.Endpoint);

            if (reply.Pdu().Variables.Count > 0)
            {
                var data = reply.Pdu().Variables[0].Data;
                //Logger.LogInformation($"SNMP raw data: {data} (Type: {data.GetType().Name})");

                // Parse the result as a number (watts)
                if (int.TryParse(data.ToString(), out var watts))
                {
                    //Logger.LogInformation($"Parsed watts as int: {watts}");
                    return watts;
                }
                if (double.TryParse(data.ToString(), out var wattsDouble))
                {
                    //Logger.LogInformation($"Parsed watts as double: {wattsDouble}");
                    return wattsDouble;
                }

                Logger.LogWarning($"Failed to parse SNMP data: '{data}'");
            }

            return null;
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Failed to query SNMP for power consumption: {ex.Message}");
            return null;
        }
    }
}
