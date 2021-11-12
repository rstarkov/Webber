using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.Sqlite;
using Webber.Shared.Blocks;

namespace Webber.Server.Blocks;

public interface IBlockServer
{
    void Init(WebApplication app);
    bool MigrateSchema(SqliteConnection db, int curVersion);
    void Start();
}

public interface IBlockServer<TDto> : IBlockServer
    where TDto : BaseDto
{
    TDto? LastUpdate { get; }
}

public static class BlockServerExtensions
{
    public static void AddBlockServer<T>(this IServiceCollection services) where T : IBlockServer
    {
        var typeIServiceWithDto = typeof(T).GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IBlockServer<>));
        services.Add(new ServiceDescriptor(typeIServiceWithDto, typeof(T), ServiceLifetime.Singleton));
        services.AddSingleton(sp => (IBlockServer) sp.GetRequiredService(typeIServiceWithDto));
    }
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
        private readonly IBlockServer<TDto> _service;
        public BlockHub(IBlockServer<TDto> service) { _service = service; }
        public override async Task OnConnectedAsync()
        {
            if (_service.LastUpdate != null)
                await Clients.Caller.Update(_service.LastUpdate);
        }
    }

    private IHubContext<BlockHub, IBlockHub> _hub;

    public TDto? LastUpdate { get; private set; }

    public abstract bool MigrateSchema(SqliteConnection db, int curVersion);
    public abstract void Start();

    public BlockServerBase(IServiceProvider sp)
    {
        _hub = sp.GetRequiredService<IHubContext<BlockHub, IBlockHub>>();
    }

    public void Init(WebApplication app)
    {
        app.MapHub<BlockHub>($"/hub/{typeof(TDto).Name.Replace("Dto", "")}");
    }

    protected void SendUpdate(TDto dto)
    {
        LastUpdate = dto;
        _hub.Clients.All.Update(dto);
    }
}
