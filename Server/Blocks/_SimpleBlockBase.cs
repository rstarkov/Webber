using Webber.Client.Models;

namespace Webber.Server.Blocks;

public abstract class SimpleBlockServerBase<TDto> : BlockServerBase<TDto>
    where TDto : BaseDto, new()
{
    private readonly TimeSpan _interval;

    public SimpleBlockServerBase(IServiceProvider sp, TimeSpan interval) : base(sp)
    {
        _interval = interval;
    }

    public SimpleBlockServerBase(IServiceProvider sp, int intervalMs) : base(sp)
    {
        _interval = TimeSpan.FromMilliseconds(intervalMs);
    }

    protected abstract TDto Tick();

    protected virtual bool ShouldTick() => IsAnyClientConnected();

    public override void Start()
    {
        new Thread(thread) { IsBackground = true }.Start();
    }

    private void thread()
    {
        while (true)
        {
            var start = DateTime.UtcNow;
#if !DEBUG
            try
#endif
            {
                if (ShouldTick())
                {
                    var update = Tick();
                    if (update != null)
                        SendUpdate(update);
                }
            }
#if !DEBUG
            catch (Exception ex)
            {
                SendUpdate((LastUpdate ?? new TDto()) with { ErrorMessage = ex.Message });
            }
#endif

            Util.SleepUntil(start + _interval);
        }
    }
}
