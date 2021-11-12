using System;
using System.IO;
using System.Threading;

namespace Webber.Server;

static class Util
{
    public static void SleepUntil(DateTime time)
    {
        var duration = time.ToUniversalTime() - DateTime.UtcNow;
        if (duration > TimeSpan.Zero)
            Thread.Sleep(duration);
    }
}
