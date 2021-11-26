using System.Reflection;
using Webber.Server;
using Webber.Server.Blocks;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = args, EnvironmentName = "Development" }); // blazor static files are broken for any other value of EnvironmentName. Use --config to load a custom appsettings.json instead.

bool help = false;
var options = new Mono.Options.OptionSet()
{
    "",
    $"Usage { Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName)} [options]",
    "",
    "Runs a SignalR server which provides statistics about this machine, local network / environment (even the weather!)",
    "",
    "Options:",
    { "h|?|help", "Shows this help", _ => help = true },
    {"hw-delete", "Uninstalls the hwinfo driver and exits.", _ => { HwInfoBlockServer.Unregister(); Environment.Exit(0); } },
    {"c=|config=", "Full path to a specific appsettings file to load.", (string path) => { builder.Configuration.AddJsonFile(path, optional: false); } },
};
options.Parse(args);

if (help)
{
    options.WriteOptionDescriptions(Console.Out);
    Environment.Exit(0);
}

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSignalR();

var blockServerTypes = Assembly.GetExecutingAssembly().GetTypes().Where(t => !t.IsAbstract && t.IsAssignableTo(typeof(IBlockServer))).ToList();
foreach (var blockServerType in blockServerTypes)
{
    var config = builder.Configuration.GetSection(blockServerType.Name.Replace("BlockServer", "Block"));
    if (!config.Exists())
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
        builder.Services.Add(new ServiceDescriptor(configType, config.Get(configType)));
}

var dbConfig = builder.Configuration.GetSection("Db").GetOrDefault<DbConfig>();
if (dbConfig == null)
    builder.Services.AddSingleton<IDbService, DisabledDbService>();
else
    builder.Services.AddSingleton<IDbService>(new DbService(dbConfig));

var app = builder.Build();

//app.UseWebAssemblyDebugging();
app.UseExceptionHandler("/Error");
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

foreach (var service in app.Services.GetServices<IBlockServer>())
    service.Init(app);

var dbService = app.Services.GetRequiredService<IDbService>();
dbService.Initialise();
foreach (var service in app.Services.GetServices<IBlockServer>())
    service.Start();

app.Run();
