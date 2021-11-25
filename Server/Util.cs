using System;
using System.IO;
using System.Threading;
using Webber.Client.Models;

namespace Webber.Server;

static class Util
{
    public static void SleepUntil(DateTime time)
    {
        var duration = time.ToUniversalTime() - DateTime.UtcNow;
        if (duration > TimeSpan.Zero)
            Thread.Sleep(duration);
    }

    public static T[] EnqueueWithMaxCapacity<T>(this Queue<T> q, T newItem, int maxCapacity)
    {
        q.Enqueue(newItem);
        while (q.Count > maxCapacity)
            q.Dequeue();
        return q.ToArray();
    }

    public static T GetOrDefault<T>(this IConfigurationSection section)
    {
        if (!section.Exists())
            return default(T);
        return section.Get<T>();
    }
}
