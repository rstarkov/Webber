namespace Webber.Client.Models;

public record WeatherBlockDto : BaseDto
{
    public decimal CurTemperature { get; set; }
    public string CurTemperatureColor { get; set; }
    public decimal MinTemperature { get; set; }
    public string MinTemperatureColor { get; set; }
    public string MinTemperatureAtTime { get; set; }
    public string MinTemperatureAtDay { get; set; }
    public decimal MaxTemperature { get; set; }
    public string MaxTemperatureColor { get; set; }
    public string MaxTemperatureAtTime { get; set; }
    public string MaxTemperatureAtDay { get; set; }
    public string SunriseTime { get; set; }
    public string SolarNoonTime { get; set; }
    public string SunsetTime { get; set; }
    public string SunsetDeltaTime { get; set; }
}
