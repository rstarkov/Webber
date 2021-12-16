using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Webber.Client;
using Webber.Client.Shared;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddSingleton<JsInterop>();

PerfTest();

await builder.Build().RunAsync();

void PerfTest()
{
    var perftest = new int[256];
    var rnd = new Random();
    var start = DateTime.UtcNow;
    int iters = 0;
    while (DateTime.UtcNow - start < TimeSpan.FromSeconds(0.1))
    {
        for (int i = 0; i < perftest.Length; i++)
            perftest[i] = rnd.Next();
        Array.Sort(perftest);
        iters++;
    }
    var time = DateTime.UtcNow - start;
    Console.WriteLine($"PerfTest: {iters} iterations in {time.TotalMilliseconds:#,0}ms ({iters / time.TotalSeconds:#,0.0} per second)");
}
