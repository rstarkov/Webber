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

    public static void EnqueueWithMaxCapacity<T>(this Queue<T> q, T newItem, int maxCapacity)
    {
        q.Enqueue(newItem);
        while (q.Count > maxCapacity)
            q.Dequeue();
    }
}
