using System.Web;
using Newtonsoft.Json;

public class SynologySrmService
{
    private readonly string _baseUrl;
    private readonly string _username;
    private readonly string _password;

    private string _sid;
    private bool _badPassword;

    public SynologySrmService(string host, int port, bool https, string username, string password)
    {
        _baseUrl = $"{(https ? "https" : "http")}://{host}:{port}/webapi/";
        _username = username;
        _password = password;
    }

    public Task<Dictionary<string, SynologyQueryInfoObject>> GetApiEndpoints() => DoRequest<Dictionary<string, SynologyQueryInfoObject>>("query.cgi", new()
    {
        {"api", "SYNO.API.Info" },
        {"version", "1" },
        {"query", "ALL" },
        {"method", "query" },
    });

    public Task<SynologyEncryptionInfo> GetEncryptionInfo() => DoRequest<SynologyEncryptionInfo>("encryption.cgi", new()
    {
        {"api", "SYNO.API.Encryption" },
        {"version", "1" },
        {"query", "ALL" },
        {"method", "getinfo" },
    });

    public Task<SynologySystemUtilization> GetSystemUtilization() => DoRequest<SynologySystemUtilization>("entry.cgi", new()
    {
        {"api", "SYNO.Core.System.Utilization" },
        {"version", "1" },
        {"method", "get" },
    });


    public Task<object> GetNgfwTrafficDomain(TrafficInterval interval = TrafficInterval.Live) => DoRequest<object>("entry.cgi", new()
    {
        {"api", "SYNO.Core.NGFW.Traffic.Domain" },
        {"version", "1" },
        {"method", "get" },
        {"interval", interval.ToString().ToLower() },
    });

    public Task<SynologyDeviceTraffic[]> GetNgfwTraffic(TrafficInterval interval = TrafficInterval.Live) => DoRequest<SynologyDeviceTraffic[]>("entry.cgi", new()
    {
        {"api", "SYNO.Core.NGFW.Traffic" },
        {"version", "1" },
        {"method", "get" },
        {"mode", "net" },
        {"interval", interval.ToString().ToLower() },
    });

    public Task<object> GetNgfwTrafficCategory(TrafficInterval interval = TrafficInterval.Live) => DoRequest<object>("entry.cgi", new()
    {
        {"api", "SYNO.Core.NGFW.Traffic" },
        {"version", "1" },
        {"method", "get" },
        {"mode", "net_17_category" },
        {"interval", interval.ToString().ToLower() },
    });

    public Task<SynologyNetworkDevices> GetNetworkNsmDevice() => DoRequest<SynologyNetworkDevices>("entry.cgi", new()
    {
        {"api", "SYNO.Core.Network.NSM.Device" },
        {"version", "5" },
        {"info", "basic" },
        {"method", "get" },
    });

    private async Task<string> GetAuthSid(string username, string password)
    {
        if (_badPassword) throw new InvalidOperationException("Please re-instantiate SynologySrmService with valid credentials.");

        var endpoint = "auth.cgi";

        Dictionary<string, string> data = new()
        {
            {"format", "sid" },
            {"account", username },
            {"passwd", password },
            {"api", "SYNO.API.Auth" },
            {"method", "login" },
            {"version", "2" },
        };

        var url = new UriBuilder(_baseUrl + endpoint);
        url.Query = string.Join("&", data.Select(kvp => $"{kvp.Key}={HttpUtility.UrlEncode(kvp.Value)}"));

        using var http = new SynologyHttpClient();

        var response = await http.GetAsync(url.Uri);
        string content = await response.Content.ReadAsStringAsync();

        var obj = JsonConvert.DeserializeObject<SynologyResponse<SynologyAuth>>(content);
        if (obj?.Success != true)
        {
            if (obj.Error?.Code is >= 400 and < 500)
            {
                // don't bother retrying requests until you create a new syn api with a new password
                _badPassword = true;
            }

            throw new Exception(obj?.Error?.ToString() ?? "Unknown Error");
        }

        return obj.Data.Sid;
    }

    private async Task<T> DoRequest<T>(string endpoint, Dictionary<string, string> data)
    {
        if (_badPassword) throw new InvalidOperationException("Please re-instantiate SynologySrmService with valid credentials.");

        var url = new UriBuilder(_baseUrl + endpoint);
        url.Query = string.Join("&", data.Select(kvp => $"{kvp.Key}={HttpUtility.UrlEncode(kvp.Value)}"));

        using var http = new SynologyHttpClient();

        bool canRetry = true;

    retry:

        var obj = await http.GetJsonAsync<SynologyResponse<T>>(url.Uri, _sid);
        if (obj?.Success != true)
        {
            // if session missing/expired, try to login and then retry the request.
            if (canRetry && obj.Error != null && obj.Error.Code is 119 or 106 or 107)
            {
                canRetry = false;
                _sid = await GetAuthSid(_username, _password);
                goto retry;
            }

            throw new Exception(obj?.Error?.ToString() ?? "Unknown Error");
        }

        return obj.Data;
    }

    private class SynologyResponse<T>
    {
        public SynologyError Error { get; set; }
        public bool Success { get; set; }
        public T Data { get; set; }
    }

    private class SynologyAuth
    {
        public string Sid { get; set; }
    }

    private class SynologyError
    {
        public int Code { get; set; }

        public override string ToString()
        {
            return Code switch
            {
                // common codes
                100 => "Unknown error",
                101 => "No parameter of API, method or version",
                102 => "The requested API does not exist",
                103 => "The requested method does not exist",
                104 => "The requested version does not support the functionality",
                105 => "The logged in session does not have permission",
                106 => "Session timeout",
                107 => "Session interrupted by duplicate login",
                117 => "Need manager rights for operation",
                119 => "Missing or invalid sid",

                // auth codes
                400 => "No account or invalid password",
                401 => "Account disabled / locked out",
                402 => "Permission denied",
                403 => "2-step verification code required",
                404 => "Failed to authenticate 2-step verification code",

                _ => $"Generic error code {Code}",
            };
        }
    }

    private class SynologyHttpClient : HttpClient
    {
        public SynologyHttpClient() : base(new HttpClientHandler { UseCookies = false, AllowAutoRedirect = true })
        {
            var os = Environment.OSVersion.Version;
            DefaultRequestHeaders.Add("User-Agent", $"Mozilla/5.0 (Windows NT {os.Major}.{os.Minor}; Win64; x64) Webber/*");
        }

        public async Task<T> GetJsonAsync<T>(Uri uri, string sid = null)
        {
            var message = new HttpRequestMessage(HttpMethod.Get, uri);

            if (sid != null)
                message.Headers.Add("Cookie", "id=" + sid);

            HttpResponseMessage response = await SendAsync(message);
            string content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(content);

            return JsonConvert.DeserializeObject<T>(content);
        }
    }
}

public enum TrafficInterval
{
    Live,
    Day,
    Week,
    Month,
}

public class SynologyEncryptionInfo
{
    [JsonProperty("cipherkey")]
    public string CipherKey { get; set; }

    [JsonProperty("ciphertoken")]
    public string CipherToken { get; set; }

    [JsonProperty("public_key")]
    public string PublicKey { get; set; }

    [JsonProperty("server_time")]
    public int ServerTime { get; set; }
}

public class SynologyQueryInfoObject
{
    public int MaxVersion { get; set; }
    public int MinVersion { get; set; }
    public string Path { get; set; }
    public string RequestFormat { get; set; }
}

public class SynologyCpu
{
    [JsonProperty("15min_load")]
    public int _15minLoad { get; set; }

    [JsonProperty("1min_load")]
    public int _1minLoad { get; set; }

    [JsonProperty("5min_load")]
    public int _5minLoad { get; set; }

    [JsonProperty("device")]
    public string Device { get; set; }

    [JsonProperty("other_load")]
    public int OtherLoad { get; set; }

    [JsonProperty("system_load")]
    public int SystemLoad { get; set; }

    [JsonProperty("user_load")]
    public int UserLoad { get; set; }
}

public class SynologySystemUtilization
{
    [JsonProperty("cpu")]
    public SynologyCpu Cpu { get; set; }

    [JsonProperty("disk")]
    public SynologyDisk Disk { get; set; }

    [JsonProperty("memory")]
    public SynologyMemory Memory { get; set; }

    [JsonProperty("network")]
    public List<SynologyNetwork> Network { get; set; }

    [JsonProperty("space")]
    public SynologyDiskSpace Space { get; set; }

    [JsonProperty("time")]
    public int Time { get; set; }
}

public class SynologyDisk
{
    [JsonProperty("disk")]
    public List<object> Disk { get; set; }

    [JsonProperty("total")]
    public SynologyTotal Total { get; set; }
}

public class SynologyMemory
{
    [JsonProperty("avail_real")]
    public int AvailReal { get; set; }

    [JsonProperty("avail_swap")]
    public int AvailSwap { get; set; }

    [JsonProperty("buffer")]
    public int Buffer { get; set; }

    [JsonProperty("cached")]
    public int Cached { get; set; }

    [JsonProperty("device")]
    public string Device { get; set; }

    [JsonProperty("memory_size")]
    public int MemorySize { get; set; }

    [JsonProperty("real_usage")]
    public int RealUsage { get; set; }

    [JsonProperty("si_disk")]
    public int SiDisk { get; set; }

    [JsonProperty("so_disk")]
    public int SoDisk { get; set; }

    [JsonProperty("swap_usage")]
    public int SwapUsage { get; set; }

    [JsonProperty("total_real")]
    public int TotalReal { get; set; }

    [JsonProperty("total_swap")]
    public int TotalSwap { get; set; }
}

public class SynologyNetwork
{
    [JsonProperty("device")]
    public string Device { get; set; }

    [JsonProperty("rx")]
    public int Rx { get; set; }

    [JsonProperty("tx")]
    public int Tx { get; set; }
}

public class SynologyDiskSpace
{
    [JsonProperty("lun")]
    public List<object> Lun { get; set; }

    [JsonProperty("total")]
    public SynologyTotal Total { get; set; }

    [JsonProperty("volume")]
    public List<SynologyVolume> Volume { get; set; }
}

public class SynologyTotal
{
    [JsonProperty("device")]
    public string Device { get; set; }

    [JsonProperty("read_access")]
    public int ReadAccess { get; set; }

    [JsonProperty("read_byte")]
    public int ReadByte { get; set; }

    [JsonProperty("utilization")]
    public int Utilization { get; set; }

    [JsonProperty("write_access")]
    public int WriteAccess { get; set; }

    [JsonProperty("write_byte")]
    public int WriteByte { get; set; }
}

public class SynologyVolume
{
    [JsonProperty("device")]
    public string Device { get; set; }

    [JsonProperty("display_name")]
    public string DisplayName { get; set; }

    [JsonProperty("read_access")]
    public int ReadAccess { get; set; }

    [JsonProperty("read_byte")]
    public int ReadByte { get; set; }

    [JsonProperty("utilization")]
    public int Utilization { get; set; }

    [JsonProperty("write_access")]
    public int WriteAccess { get; set; }

    [JsonProperty("write_byte")]
    public int WriteByte { get; set; }
}

// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
public class SynologyNetworkDevices
{
    [JsonProperty("devices")]
    public List<SynologyDevice> Devices { get; set; }

    [JsonProperty("exceed_dev_list_max")]
    public bool ExceedDevListMax { get; set; }
}

public class SynologyDevice
{
    [JsonProperty("dev_type")]
    public string DevType { get; set; }

    [JsonProperty("hostname")]
    public string Hostname { get; set; }

    [JsonProperty("ip6_addr")]
    public string Ip6Addr { get; set; }

    [JsonProperty("ip_addr")]
    public string IpAddr { get; set; }

    [JsonProperty("is_online")]
    public bool IsOnline { get; set; }

    [JsonProperty("is_wireless")]
    public bool IsWireless { get; set; }

    [JsonProperty("mac")]
    public string Mac { get; set; }

    [JsonProperty("mesh_node_id")]
    public int MeshNodeId { get; set; }

    [JsonProperty("mesh_node_name")]
    public string MeshNodeName { get; set; }

    [JsonProperty("network")]
    public string Network { get; set; }

    [JsonProperty("band")]
    public string Band { get; set; }

    [JsonProperty("current_rate")]
    public int? CurrentRate { get; set; }

    [JsonProperty("is_guest")]
    public bool? IsGuest { get; set; }

    [JsonProperty("max_rate")]
    public int? MaxRate { get; set; }

    [JsonProperty("rate_quality")]
    public string RateQuality { get; set; }

    [JsonProperty("signalstrength")]
    public int? SignalStrength { get; set; }

    [JsonProperty("transferRXRate")]
    public int? TransferRXRate { get; set; }

    [JsonProperty("transferTXRate")]
    public int? TransferTXRate { get; set; }

    [JsonProperty("wifi_network_id")]
    public int? WifiNetworkId { get; set; }

    [JsonProperty("wifi_profile_name")]
    public string WifiProfileName { get; set; }

    [JsonProperty("wifi_ssid")]
    public string WifiSsid { get; set; }

    public override string ToString()
    {
        var network = IsWireless ? Band : "LAN";

        return $"{Hostname} ({IpAddr} [{network}])";
    }
}

// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
public class SynologyDeviceTraffic
{
    [JsonProperty("deviceID")]
    public string DeviceID { get; set; }

    [JsonProperty("download")]
    public int Download { get; set; }

    [JsonProperty("download_packets")]
    public int DownloadPackets { get; set; }

    [JsonProperty("recs")]
    public List<SynologyRec> Recs { get; set; }

    [JsonProperty("upload")]
    public int Upload { get; set; }

    [JsonProperty("upload_packets")]
    public int UploadPackets { get; set; }
}

public class SynologyProtocolList
{
    [JsonProperty("download")]
    public int Download { get; set; }

    [JsonProperty("download_packets")]
    public int DownloadPackets { get; set; }

    [JsonProperty("protocol")]
    public int Protocol { get; set; }

    [JsonProperty("upload")]
    public int Upload { get; set; }

    [JsonProperty("upload_packets")]
    public int UploadPackets { get; set; }
}

public class SynologyRec
{
    [JsonProperty("download")]
    public int Download { get; set; }

    [JsonProperty("download_packets")]
    public int DownloadPackets { get; set; }

    [JsonProperty("protocollist")]
    public List<SynologyProtocolList> Protocollist { get; set; }

    [JsonProperty("time")]
    public int Time { get; set; }

    [JsonProperty("upload")]
    public int Upload { get; set; }

    [JsonProperty("upload_packets")]
    public int UploadPackets { get; set; }
}
