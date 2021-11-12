using Webber.Server.Blocks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddSignalR();
builder.Services.AddSingleton(new PingBlockConfig());
builder.Services.AddBlockServer<PingBlockServer>();

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
