using Microsoft.Data.Sqlite;
using Webber.Client.Models;

namespace Webber.Server.Blocks;

public abstract class SimpleBlockServerBase<TDto> : BlockServerBase<TDto>
    where TDto : BaseDto
{
    private readonly int _intervalMs;

    public SimpleBlockServerBase(IServiceProvider sp, int intervalMs) : base(sp)
    {
        _intervalMs = intervalMs;
    }

    public abstract TDto Tick();

    public override void Start()
    {
        new Thread(thread) { IsBackground = true }.Start();
    }

    private void thread()
    {
        while (true)
        {
            var start = DateTime.UtcNow;
            try
            {
                if (IsAnyClientConnected())
                    SendUpdate(Tick());
            }
            catch
            {
                // todo: capture error? can we send to client if signalr is working? or log to console?
            }

            Util.SleepUntil(start.AddMilliseconds(_intervalMs));
        }
    }
}
