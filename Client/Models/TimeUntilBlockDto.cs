﻿namespace Webber.Client.Models;

public record CalendarEvent
{
    public string DisplayName { get; set; }
    //public string DisplayColor { get; set; }
    public DateTime Time { get; set; }
}

public record TimeUntilBlockDto : BaseDto
{
    public CalendarEvent[] Events { get; set; }
}