using TimeZoneConverter;

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

    private static DateTime _unixepoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static uint ToUnixSeconds(this DateTime time)
    {
        return (uint)(time - _unixepoch).TotalSeconds;
    }

    public static DateTime FromUnixSeconds(this uint time)
    {
        return _unixepoch.AddSeconds(time);
    }

    public static double GetUtcOffset(string timezoneName)
    {
        // var timezones = TZConvert.KnownIanaTimeZoneNames;
        return TZConvert.GetTimeZoneInfo(timezoneName).GetUtcOffset(DateTimeOffset.UtcNow).TotalHours;
    }
}
