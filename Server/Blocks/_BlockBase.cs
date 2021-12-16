using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Webber.Client.Models;

namespace Webber.Server.Blocks;

public interface IBlockServer
{
    void Init(WebApplication app);
    void Start();
}

public interface IBlockServer<TDto> : IBlockServer
    where TDto : BaseDto
{
    TDto LastUpdate { get; }
}

public abstract class BlockServerBase<TDto> : IBlockServer<TDto>
    where TDto : BaseDto
{
    public interface IBlockHub
    {
        Task Update(TDto dto);
    }

    public class BlockHub : Hub<IBlockHub>
    {
        //public int NumberOfConnections => _service._connectedIds.Count;

        private readonly IBlockServer<TDto> _service;

        public BlockHub(IBlockServer<TDto> service) { _service = service; }

        public override async Task OnConnectedAsync()
        {
            //_connectedIds.Add(Context.ConnectionId);
            if (_service.LastUpdate != null)
                await Clients.Caller.Update(_service.LastUpdate);

            await base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            //_connectedIds.Remove(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }
    }

    private IHubContext<BlockHub, IBlockHub> _hub;
    private AppConfig _config;
    private ConcurrentBag<string> _connectedIds = new ConcurrentBag<string>();

    public TDto LastUpdate { get; private set; }

    public abstract void Start();

    public BlockServerBase(IServiceProvider sp)
    {
        _hub = sp.GetRequiredService<IHubContext<BlockHub, IBlockHub>>();
        _config = sp.GetRequiredService<AppConfig>();
    }

    public void Init(WebApplication app)
    {
        app.MapHub<BlockHub>($"/hub/{typeof(TDto).Name.Replace("Dto", "")}");
    }

    protected void SendUpdate(TDto dto)
    {
        dto.SentUtc = DateTime.UtcNow;
        dto.LocalOffsetHours = Util.GetUtcOffset(_config.LocalTimezoneName);
        LastUpdate = dto;
        _hub.Clients.All.Update(dto);
    }

    protected bool IsAnyClientConnected()
    {
        return true;
        //return _hub.
    }
}
