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
            try
            {
                if (ShouldTick())
                {
                    var update = Tick();
                    if (update != null)
                        SendUpdate(update);
                }
            }
            catch (TellUserException ex)
            {
                SendUpdate(new TDto { ErrorMessage = ex.Message });
            }
#if !DEBUG
            catch (Exception ex)
            {
                Logger.LogError(ex, "Unhandled exception");
                SendUpdate((LastUpdate ?? new TDto()) with { ErrorMessage = ex.Message });
            }
#endif

            Util.SleepUntil(start + _interval);
        }
    }
}

public class TellUserException : Exception
{
    public TellUserException(string message)
        : base(message)
    { }
}
