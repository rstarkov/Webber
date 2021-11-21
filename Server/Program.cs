using System.Reflection;
using Webber.Server.Blocks;

var builder = WebApplication.CreateBuilder(args);

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
    builder.Services.Add(new ServiceDescriptor(IBlockServer_TDto, blockServerType, ServiceLifetime.Singleton));
    builder.Services.AddSingleton(sp => (IBlockServer) sp.GetRequiredService(IBlockServer_TDto));
    // Register its configuration
    var configTypeName = blockServerType.FullName.Replace("BlockServer", "BlockConfig");
    var configType = Assembly.GetExecutingAssembly().GetType(configTypeName);
    if (configType != null)
        builder.Services.Add(new ServiceDescriptor(configType, config.Get(configType)));
}

var options = new Mono.Options.OptionSet()
{
    {"hw-delete", (_) => { HwInfoBlockServer.Unregister(); Environment.Exit(0); } }
};

options.Parse(args);

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseWebAssemblyDebugging();
else
    app.UseExceptionHandler("/Error");

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

foreach (var service in app.Services.GetServices<IBlockServer>())
{
    service.Init(app);
    //service.MigrateSchema();
    service.Start();
}

app.Run();
