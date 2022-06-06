using System.Reflection;
using Topshelf;
using Webber.Server.Blocks;
using Webber.Server.Services;

namespace Webber.Server;

class AppConfig
{
    public string DbFilePath { get; init; }
    public string LocalTimezoneName { get; init; }
    public bool DisableCaching { get; init; }
}

class WebberService : ServiceControl
{
    private readonly string _configPath;
    private WebApplication app;

    public WebberService(string configPath)
    {
        if (string.IsNullOrWhiteSpace(configPath) || !File.Exists(configPath))
            throw new ArgumentException("The '-config' command line variable is required");
        this._configPath = configPath;

        Directory.SetCurrentDirectory(AppContext.BaseDirectory);

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            // blazor static files are broken for any other value of EnvironmentName. Use --config to load a custom appsettings.json instead.
            EnvironmentName = "Development",
            /*Args = args,*/
            ContentRootPath = AppContext.BaseDirectory, // arrg
        });

        builder.Configuration.AddJsonFile(_configPath, optional: false);

        builder.Logging.AddConsole2(); // disabled by default; enabled/configured in config JSON
        builder.Logging.AddFile(); // disabled by default; enabled/configured in config JSON
        builder.Services.AddControllers();
        builder.Services.AddSignalR();

        var blockServerTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => !t.IsAbstract && t.IsAssignableTo(typeof(IBlockServer))).ToList();
        foreach (var blockServerType in blockServerTypes)
        {
            var blockConfig = builder.Configuration.GetSection(blockServerType.Name.Replace("BlockServer", "Block"));
            if (!blockConfig.Exists())
                continue;

            // Register the block server
            var IBlockServer_TDto = blockServerType.GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IBlockServer<>));
            builder.Services.Add(new ServiceDescriptor(blockServerType, blockServerType, ServiceLifetime.Singleton));

            // Register it's interfaces to use the same instance
            Func<IServiceProvider, object> factory = (sp) => sp.GetRequiredService(blockServerType);
            builder.Services.Add(new ServiceDescriptor(IBlockServer_TDto, factory, ServiceLifetime.Singleton));
            builder.Services.Add(new ServiceDescriptor(typeof(IBlockServer), factory, ServiceLifetime.Singleton));

            // Register its configuration
            var configTypeName = blockServerType.FullName.Replace("BlockServer", "BlockConfig");
            var configType = Assembly.GetExecutingAssembly().GetType(configTypeName);
            if (configType != null)
                builder.Services.Add(new ServiceDescriptor(configType, blockConfig.Get(configType)));
        }

        var config = builder.Configuration.GetSection("App").GetOrDefault<AppConfig>();
        builder.Services.AddSingleton<AppConfig>(config);

        // TODO make this just a single service which toggles "Enabled" on the presence of DbFilePath
        if (string.IsNullOrEmpty(config.DbFilePath))
            builder.Services.AddSingleton<IDbService, DisabledDbService>();
        else
            builder.Services.AddSingleton<IDbService>(new DbService(config));

        app = builder.Build();
        app.Logger.LogInformation($"Webber starting");

        if (config.DisableCaching)
            app.UseMiddleware<NoCacheHeadersMiddleware>();
        app.UseCors(b => { b.WithOrigins("http://localhost:3000").AllowAnyHeader().WithMethods("GET", "POST").AllowCredentials(); });
        app.UseStaticFiles();
        app.UseRouting();
        app.MapControllers();
        app.MapFallbackToFile("index.html");
    }

    public bool Start(HostControl hostControl)
    {
        foreach (var service in app.Services.GetServices<IBlockServer>())
            service.Init(app);

        var dbService = app.Services.GetRequiredService<IDbService>();
        dbService.Initialise();

        foreach (var service in app.Services.GetServices<IBlockServer>())
            service.Start();

        app.Start();
        return true;
    }

    public bool Stop(HostControl hostControl)
    {
        //foreach (var service in app.Services.GetServices<IBlockServer>())
        //    service.Stop();

        app.StopAsync().Wait();
        return true;
    }
}
