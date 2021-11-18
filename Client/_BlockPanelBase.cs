using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Webber.Client.Models;

namespace Webber.Client;

public class BlockPanelBase<TDto> : ComponentBase, IAsyncDisposable
    where TDto : BaseDto
{
    private HubConnection _hubConnection;
    private CancellationTokenSource _cts = new();
    [Inject]
    private NavigationManager _navigationManager { get; set; }
    private System.Timers.Timer _invalidTimer = new System.Timers.Timer { AutoReset = false };

    protected bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;
    protected TDto LastUpdate { get; private set; }

    protected override async Task OnInitializedAsync()
    {
        _invalidTimer.Elapsed += OnInvalidTimer;
        _invalidTimer.Enabled = false;
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(_navigationManager!.ToAbsoluteUri($"/hub/{typeof(TDto).Name.Replace("Dto", "")}"))
            .Build();
        _hubConnection.Closed += OnConnectionLost;
        _hubConnection.On<TDto>("Update", OnUpdateReceived);
        await ConnectWithRetryAsync();
        await base.OnInitializedAsync();
    }

    private void OnUpdateReceived(TDto dto)
    {
        LastUpdate = dto;
        _invalidTimer.Interval = Math.Max(1, (dto.ValidUntilUtc - DateTime.UtcNow).TotalMilliseconds);
        _invalidTimer.Enabled = true;
        StateHasChanged();
    }

    private void OnInvalidTimer(object sender, System.Timers.ElapsedEventArgs e)
    {
        if (DateTime.UtcNow > LastUpdate!.ValidUntilUtc)
        {
            _invalidTimer.Enabled = false;
            StateHasChanged();
        }
        else
        {
            // It has fired before the validity interval has expired, in which case postpone it instead of disabling
            _invalidTimer.Interval = Math.Max(1, (LastUpdate.ValidUntilUtc - DateTime.UtcNow).TotalMilliseconds) + 100;
            _invalidTimer.Enabled = true;
        }
    }

    private async Task OnConnectionLost(Exception e)
    {
        StateHasChanged();
        await ConnectWithRetryAsync();
        StateHasChanged();
    }

    private async Task ConnectWithRetryAsync()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                await _hubConnection!.StartAsync(_cts.Token);
                return;
            }
            catch { }
            await Task.Delay(5000);
        }
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        _cts.Cancel();
        _cts.Dispose();
        _invalidTimer.Dispose();
        await _hubConnection!.DisposeAsync();
    }
}
