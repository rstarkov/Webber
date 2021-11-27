using Microsoft.JSInterop;

namespace Webber.Client.Shared;

public class JsInterop
{
    private IJSRuntime _runtime;
    private IJSObjectReference _module;
    private Task _import;

    public JsInterop(IJSRuntime runtime)
    {
        _runtime = runtime;
        _import = _runtime.InvokeAsync<IJSObjectReference>("import", "./js/interop.js").AsTask().ContinueWith(t => { _module = t.Result; });
    }

    public async Task EnterFullscreen()
    {
        await _import;
        await _module.InvokeVoidAsync("enterFullscreen");
    }

    public async Task ExitFullscreen()
    {
        await _import;
        await _module.InvokeVoidAsync("exitFullscreen");
    }
}
