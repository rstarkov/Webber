using Topshelf;
using Webber.Server;

var envDocker = Environment.GetEnvironmentVariable("WEBBER_DOCKER");
var envConfig = Environment.GetEnvironmentVariable("WEBBER_CONFIG");

if (!string.IsNullOrEmpty(envDocker))
{
    if (string.IsNullOrEmpty(envConfig))
        throw new Exception("Must specify config path via the WEBBER_CONFIG argument in docker containers.");

    var svc = new WebberService(envConfig);
    return svc.StartAndBlock();
}


if (args.Length == 2 && args[0] == "--debug")
{
    var svc = new WebberService(args[1]);
    return svc.StartAndBlock();
}

string configPath = null;

return (int)HostFactory.Run(host =>
{
    host.AddPersistedCommandLineArgument("config", c => configPath = c);
    host.Service<WebberService>((d) => new WebberService(configPath));
    host.SetServiceName("WebberServer");
    host.SetDisplayName("WebberServer");
    host.SetDescription("A local signalr service for webber client dashboards");
    host.EnableShutdown();
    host.EnableServiceRecovery(r =>
    {
        r.RestartService(0); // try to restart immediately
        r.RestartService(1); // then, try to restart after one minute
        r.RestartService(10); // then, try to restart the service every 10 minutes after that
    });
});
