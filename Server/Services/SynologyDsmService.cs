using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RT.Util.ExtensionMethods;

public class SynologDsmService
{
    private readonly string _baseUrl;
    private readonly string _username;
    private readonly string _password;

    private string _sid;
    private bool _badPassword;

    public SynologDsmService(string host, int port, bool https, string username, string password)
    {
        _baseUrl = $"{(https ? "https" : "http")}://{host}:{port}/webapi/";
        _username = username;
        _password = password;
    }

    public async Task<SynologyDSTaskList> GetTopDownloadStationTasks()
    {
        List<Dictionary<string, string>> arguments = new()
        {
            new()
            {
                {"api", "SYNO.DownloadStation2.Task" },
                {"version", "2" },
                {"method", "list" },
                {"offset", "0" },
                {"limit", "3" },
                {"sort_by", "DESC" },
                {"order", "upload_rate" },
                {"additional", JsonConvert.SerializeObject(new string[] { "transfer" }) },
                {"type", JsonConvert.SerializeObject(new string[] { "emule" }) },
                {"type_inverse", "true" },
            },
            new()
            {
                {"api", "SYNO.DownloadStation2.Task" },
                {"version", "2" },
                {"method", "list" },
                {"offset", "0" },
                {"limit", "3" },
                {"sort_by", "DESC" },
                {"order", "current_rate" },
                {"additional", JsonConvert.SerializeObject(new string[] { "transfer" }) },
                {"type", JsonConvert.SerializeObject(new string[] { "emule" }) },
                {"type_inverse", "true" },
            },
        };

        var compound = JsonConvert.SerializeObject(arguments);
        var response = await DoRequest<JToken>("entry.cgi", new()
        {
            {"api", "SYNO.Entry.Request" },
            {"stop_when_error", "false" },
            {"mode", "parallel" },
            {"version", "1" },
            {"method", "request" },
            {"compound", compound },
        }, true);

        var results = response["result"].ToArray();

        var upload = results[0].ToObject<SynologyDSTaskList>();
        var download = results[1].ToObject<SynologyDSTaskList>();

        var list = from task in upload.Tasks.Concat(download.Tasks)
                   let trans = task.Additional.Transfer
                   where trans.SpeedDownload > 0 || trans.SpeedUpload > 0 || (trans.SizeDownloaded < task.Size)
                   orderby Math.Max(trans.SpeedDownload, trans.SpeedUpload) descending, (trans.SizeDownloaded / task.Size)
                   select task;

        return new SynologyDSTaskList { Tasks = list.Take(3).ToList() };
    }

    public Task<SynologyDSTaskList> GetDownloadStationTasks(
        int offset = 0, int limit = 25, string sortby = "upload_rate", string order = "DESC")
        => DoRequest<SynologyDSTaskList>("entry.cgi", new()
        {
            {"api", "SYNO.DownloadStation2.Task" },
            {"version", "2" },
            {"method", "list" },
            {"offset", offset.ToString() },
            {"limit", limit.ToString() },
            {"sort_by", sortby },
            {"order", order },
            {"additional", JsonConvert.SerializeObject(new string[] { "transfer" }) },
            {"type", JsonConvert.SerializeObject(new string[] { "emule" }) },
            {"type_inverse", "true" },
        }, true);

    private async Task<string> GetAuthSid(string username, string password)
    {
        if (_badPassword) throw new InvalidOperationException("Please re-instantiate SynologySrmService with valid credentials.");

        var endpoint = "entry.cgi";

        Dictionary<string, string> data = new()
        {
            {"account", username },
            {"passwd", password },
            {"api", "SYNO.API.Auth" },
            {"method", "login" },
            {"version", "6" },
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

    private async Task<T> DoRequest<T>(string endpoint, Dictionary<string, string> queryData, bool postForm = false)
    {
        if (_badPassword) throw new InvalidOperationException("Please re-instantiate SynologySrmService with valid credentials.");

        var url = new UriBuilder(_baseUrl + endpoint);
        HttpContent req = null;

        if (postForm)
        {
            req = new FormUrlEncodedContent(queryData);
        }
        else
        {
            url.Query = string.Join("&", queryData.Select(kvp => $"{kvp.Key}={HttpUtility.UrlEncode(kvp.Value)}"));
        }

        using var http = new SynologyHttpClient();
        bool canRetry = true;

    retry:
        var json = await http.SendJsonRequestAsync(url.Uri, req, _sid);

        List<SynologyError> errors = new();
        var root = JObject.Parse(json);
        var success = root["success"].ToObject<bool>();
        if (!success)
        {
            var rootError = root["error"].ToObject<SynologyError>();
            errors.Add(rootError);
        }
        else if (root.ContainsKey("data"))
        {
            var dataObj = root["data"] as JObject;
            if (dataObj != null && dataObj.ContainsKey("has_fail") && root["data"]["has_fail"].ToObject<bool>())
            {
                var dataResult = root["data"]["result"].ToArray();
                foreach (var d in dataResult)
                {
                    var dataSuccess = d["success"].ToObject<bool>();
                    if (!dataSuccess)
                    {
                        var dataError = d["error"].ToObject<SynologyError>();
                        errors.Add(dataError);
                    }
                }
            }
        }

        if (errors.Count > 0)
        {
            if (canRetry)
            {
                foreach (var e in errors)
                {
                    if (e.Code is 119 or 106 or 107)
                    {
                        canRetry = false;
                        _sid = await GetAuthSid(_username, _password);
                        goto retry;
                    }
                }
            }

            if (errors.Count == 1)
                throw new Exception(errors.First().ToString());
            throw new AggregateException(errors.Select(e => new Exception(e.ToString())));
        }

        return root["data"].ToObject<T>();
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
            var content = await SendJsonRequestAsync(uri, null, sid);
            return JsonConvert.DeserializeObject<T>(content);
        }

        public async Task<string> SendJsonRequestAsync(Uri uri, HttpContent requestContent = null, string sid = null)
        {
            var message = new HttpRequestMessage(requestContent != null ? HttpMethod.Post : HttpMethod.Get, uri);
            if (requestContent != null)
                message.Content = requestContent;

            if (sid != null)
                message.Headers.Add("Cookie", "id=" + sid);

            HttpResponseMessage response = await SendAsync(message);
            string content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(content);

            return content;
        }
    }
}


// Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
public class SynologyDSAdditionalTaskInfo
{
    [JsonProperty("detail")]
    public SynologyDSTaskDetail Detail { get; set; }

    [JsonProperty("transfer")]
    public SynologyDSTransferInfo Transfer { get; set; }
}

public class SynologyDSTaskList
{
    [JsonProperty("task")]
    public List<SynologyDSTask> Tasks { get; set; }
}

public class SynologyDSTaskDetail
{
    [JsonProperty("completed_time")]
    public int CompletedTime { get; set; }

    [JsonProperty("connected_leechers")]
    public int ConnectedLeechers { get; set; }

    [JsonProperty("connected_peers")]
    public int ConnectedPeers { get; set; }

    [JsonProperty("connected_seeders")]
    public int ConnectedSeeders { get; set; }

    [JsonProperty("created_time")]
    public int CreatedTime { get; set; }

    [JsonProperty("destination")]
    public string Destination { get; set; }

    [JsonProperty("extract_password")]
    public string ExtractPassword { get; set; }

    [JsonProperty("seed_elapsed")]
    public int SeedElapsed { get; set; }

    [JsonProperty("started_time")]
    public int StartedTime { get; set; }

    [JsonProperty("total_peers")]
    public int TotalPeers { get; set; }

    [JsonProperty("total_pieces")]
    public int TotalPieces { get; set; }

    [JsonProperty("uri")]
    public string Uri { get; set; }

    [JsonProperty("waiting_seconds")]
    public int WaitingSeconds { get; set; }
}

public class SynologyDSTask
{
    [JsonProperty("additional")]
    public SynologyDSAdditionalTaskInfo Additional { get; set; }

    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("size")]
    public long Size { get; set; }

    [JsonProperty("status")]
    public int Status { get; set; }

    [JsonProperty("title")]
    public string Title { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("username")]
    public string Username { get; set; }
}

public class SynologyDSTransferInfo
{
    [JsonProperty("downloaded_pieces")]
    public int DownloadedPieces { get; set; }

    [JsonProperty("size_downloaded")]
    public long SizeDownloaded { get; set; }

    [JsonProperty("size_uploaded")]
    public long SizeUploaded { get; set; }

    [JsonProperty("speed_download")]
    public int SpeedDownload { get; set; }

    [JsonProperty("speed_upload")]
    public int SpeedUpload { get; set; }
}

