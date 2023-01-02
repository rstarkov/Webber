﻿namespace Webber.Client.Models;

public record WeatherDotComBlockDto : BaseDto
{
    public WeatherDotComForecastHourDto[] Hours { get; set; }
}

public record WeatherDotComForecastHourDto
{
    public DateTime DateTime { get; set; }
    public int CloudCover { get; set; } // 0..100
    public int PrecipChance { get; set; } // 0..100
    public double PrecipMm { get; set; } // melted mm if snow
}
