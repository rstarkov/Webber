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

    // Optional SNMP settings for power consumption from UPS
    public string SnmpHost { get; set; }
    public string SnmpUser { get; set; }
    public string SnmpPassword { get; set; }
    public string SnmpOid { get; set; } = "1.3.6.1.4.1.534.1.4.4.1.4.1"; // Default OID for Eaton UPS output watts
}

class ComputerStatsBlockServer : SimpleBlockServerBase<ComputerStatsBlockDto>
{
    private ComputerStatsBlockConfig _config;
    private HttpClient _httpClient = new();
    private Dictionary<string, SnmpConnection> _snmpConnections = new();
    private Dictionary<string, ulong> _totalRamByComputer = new();

    public ComputerStatsBlockServer(IServiceProvider sp, ComputerStatsBlockConfig config)
        : base(sp, config.IntervalMs)
    {
        _config = config;

        // Set aggressive timeout for all HTTP requests (same as interval)
        _httpClient.Timeout = TimeSpan.FromMilliseconds(config.IntervalMs);

        // Initialize SNMP connections for each computer
        foreach (var computer in config.Computers)
        {
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

            // Fetch and cache total RAM from /info endpoint
            if (!string.IsNullOrWhiteSpace(computer.MonitoringUrl))
            {
                try
                {
                    var infoUrl = $"{computer.MonitoringUrl.TrimEnd('/')}/info";
                    var infoResponse = _httpClient.GetStringAsync(infoUrl).GetAwaiter().GetResult();
                    var info = JsonConvert.DeserializeObject<SystemInfo>(infoResponse);
                    if (info?.Ram?.Size > 0)
                    {
                        _totalRamByComputer[computer.Name] = info.Ram.Size;
                        Logger.LogInformation($"Cached total RAM for '{computer.Name}': {info.Ram.Size / (1024 * 1024 * 1024)} GB");
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning($"Failed to fetch total RAM for '{computer.Name}': {ex.Message}");
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
                if (!string.IsNullOrWhiteSpace(computerConfig.MonitoringUrl))
                {
                    var cpuUrl = $"{computerConfig.MonitoringUrl.TrimEnd('/')}/load/cpu";
                    var cpuResponse = _httpClient.GetStringAsync(cpuUrl)
                        .GetAwaiter().GetResult();
                    var cpuData = JsonConvert.DeserializeObject<List<CpuCoreInfo>>(cpuResponse);
                    computerStats.CpuCores = cpuData ?? new List<CpuCoreInfo>();
                }

                // Fetch GPU data
                if (!string.IsNullOrWhiteSpace(computerConfig.MonitoringUrl))
                {
                    var gpuUrl = $"{computerConfig.MonitoringUrl.TrimEnd('/')}/load/gpu";
                    var gpuResponse = _httpClient.GetStringAsync(gpuUrl)
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

                // Fetch power consumption via SNMP (optional)
                if (_snmpConnections.TryGetValue(computerConfig.Name, out var snmpConn))
                {
                    computerStats.PowerConsumptionWatts = GetPowerConsumptionSnmp(snmpConn);
                }

                // Fetch RAM usage and calculate percentage (optional)
                if (!string.IsNullOrWhiteSpace(computerConfig.MonitoringUrl) &&
                    _totalRamByComputer.TryGetValue(computerConfig.Name, out var totalRam))
                {
                    try
                    {
                        var ramLoadUrl = $"{computerConfig.MonitoringUrl.TrimEnd('/')}/load/ram";
                        var ramLoadResponse = _httpClient.GetStringAsync(ramLoadUrl)
                            .GetAwaiter().GetResult();
                        var ramLoad = JsonConvert.DeserializeObject<RamLoadInfo>(ramLoadResponse);

                        if (ramLoad?.Load > 0 && totalRam > 0)
                        {
                            computerStats.RamUtilization = ((double)ramLoad.Load / totalRam) * 100;
                        }
                    }
                    catch (Exception ramEx)
                    {
                        Logger.LogWarning($"Failed to fetch RAM load for '{computerConfig.Name}': {ramEx.Message}");
                    }
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
