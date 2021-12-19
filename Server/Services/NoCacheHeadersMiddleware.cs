using Microsoft.Extensions.Primitives;

namespace Webber.Server.Services;

public class NoCacheHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public NoCacheHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.CacheControl = new StringValues(new[] { "no-cache", "no-store" });
            context.Response.Headers.Expires = new StringValues("-1");
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
