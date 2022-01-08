using Topshelf;
using Webber.Server;

if (args.Length == 2 && args[0] == "--debug")
{
    var svc = new WebberService(args[1]);
    svc.Start(null);
    while (true)
        Thread.Sleep(1000);
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
