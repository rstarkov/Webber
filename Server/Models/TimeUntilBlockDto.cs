﻿namespace Webber.Client.Models;

public record CalendarEvent
{
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public bool HasStarted { get; set; }
    public bool IsNextUp { get; set; }
    public bool IsRecurring { get; set; }
    public bool IsAllDay { get; set; }
}

public record TimeUntilBlockDto : BaseDto
{
    public CalendarEvent[] Events { get; set; }
}
