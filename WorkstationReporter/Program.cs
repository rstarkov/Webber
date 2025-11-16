using WorkstationReporter.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure port from command line or use default
var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 54344;
builder.WebHost.UseUrls($"http://*:{port}");

// Register the stats collector service
builder.Services.AddSingleton<StatsCollectorService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<StatsCollectorService>());

var app = builder.Build();
Console.WriteLine($"WorkstationReporter listening on port {port}");

// Get the stats collector service
var statsCollector = app.Services.GetRequiredService<StatsCollectorService>();

app.MapGet("/load/cpu", () => statsCollector.GetCpuStats());

app.MapGet("/load/gpu", () => statsCollector.GetGpuStats());

app.MapGet("/load/ram", () => statsCollector.GetRamStats());

app.MapGet("/info", () => statsCollector.GetSystemInfo());

app.Run();
