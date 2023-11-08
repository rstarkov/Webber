﻿namespace Webber.Client.Models;

public record CalendarEvent
{
    public string DisplayName { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public bool HasStarted { get; set; }
    public bool IsNextUp { get; set; }
    public bool IsRecurring { get; set; }
    public bool IsAllDay { get; set; }
    public bool SpecialEvent { get; set; }
}

public record TimeUntilBlockDto : BaseDto
{
    public CalendarEvent[] Events { get; set; }
}
