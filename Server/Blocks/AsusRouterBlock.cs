using System.Net;
using System.Text.RegularExpressions;
using Webber.Client.Models;
using Webber.Server;
using Webber.Server.Blocks;

class AsusRouterBlockConfig : RouterBlockConfig
{
    public string LoginAuth { get; set; } = "base64(user:pass)";
    public int SleepOnParallelSec { get; set; } = 5 * 60;
    public int SleepOnErrorSec { get; set; } = 60;
}

class AsusRouterBlockServer : RouterBlockServerBase<RouterBlockDto, AsusRouterBlockConfig>
{
    private HttpClient _httpClient;

    public AsusRouterBlockServer(IServiceProvider sp, AsusRouterBlockConfig config, IDbService db) : base(sp, config, db)
    {
    }

    private void login()
    {
        Logger.LogDebug("Logging in to router UI");
        var handler = new HttpClientHandler();
        _httpClient = new HttpClient(handler); // we got logged out, so start from scratch just in case
        var req = new HttpRequestMessage(HttpMethod.Post, Config.BaseUrl + "/login.cgi");
        req.Headers.Referrer = new Uri(Config.BaseUrl + "/Main_Login.asp");
        req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["group_id"] = "",
            ["action_mode"] = "",
            ["action_script"] = "",
            ["action_wait"] = "5",
            ["current_page"] = "Main_Login.asp",
            ["next_page"] = "index.asp",
            ["login_authorization"] = Config.LoginAuth,
        });
        var resp = _httpClient.Send(req);
        if (!resp.IsSuccessStatusCode)
            throw new TellUserException("Router login failed");
        var cookies = handler.CookieContainer.GetCookies(new Uri(Config.BaseUrl + "/login.cgi"));
        if (cookies.Count == 0)
            throw new TellUserException("Router login failed");
        handler.CookieContainer.Add(new Uri(Config.BaseUrl), new Cookie("asus_token", handler.CookieContainer.GetCookies(new Uri(Config.BaseUrl + "/login.cgi"))[0].Value));
        handler.CookieContainer.Add(new Uri(Config.BaseUrl), new Cookie("bw_rtab", "INTERNET"));
        handler.CookieContainer.Add(new Uri(Config.BaseUrl), new Cookie("traffic_warning_0", "2017.7:1"));
    }

    protected virtual RouterHistoryPoint GetNextHistoryPoint()
    {
        var pt = new RouterHistoryPoint();

        try
        {
            var req = new HttpRequestMessage(HttpMethod.Post, Config.BaseUrl + "/update.cgi");
            req.Headers.Referrer = new Uri(Config.BaseUrl + "/Main_TrafficMonitor_realtime.asp");
            req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["output"] = "netdev",
                ["_http_id"] = "TIDe855a6487043d70a",
            });
            var resp = _httpClient.Send(req).EnsureSuccessStatusCode();
            var respStr = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            pt.Timestamp = DateTime.UtcNow;

            // When kicked out, the response looks something like this: <HTML><HEAD><script>top.location.href='/Main_Login.asp';</script></HEAD></HTML>
            if (respStr.Contains("location.href='/Main_Login.asp'"))
            {
                Logger.LogInformation("Parallel login detected; sleeping.");
                SendUpdate(LastUpdate with { ErrorMessage = "Parallel login detected; sleeping." });
                Thread.Sleep(TimeSpan.FromSeconds(Config.SleepOnParallelSec));
                login();
                return null;
            }

            var match = Regex.Match(respStr, @"'INTERNET':{rx:0x(?<rx>.*?),tx:0x(?<tx>.*?)}");
            if (!match.Success)
                throw new Exception();

            pt.TxTotal = Convert.ToInt64(match.Groups["tx"].Value, 16);
            pt.RxTotal = Convert.ToInt64(match.Groups["rx"].Value, 16);
        }
        catch (Exception ex)
        {
            Logger.LogDebug(ex, "Pausing due to exception while screen scraping:");
            Thread.Sleep(TimeSpan.FromSeconds(Config.SleepOnErrorSec));
            login();
            return null;
        }

        return pt;
    }

    protected override RouterBlockDto Tick()
    {
        if (_httpClient == null)
            login();

        var pt = GetNextHistoryPoint();
        if (pt == null)
            return null;

        return ProcessHistoryPoint(pt);
    }
}
