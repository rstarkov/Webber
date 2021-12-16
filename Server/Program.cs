using Topshelf;
using Webber.Server;

string configPath = null;

Environment.ExitCode = (int) HostFactory.Run(host =>
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
