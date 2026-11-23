namespace WeatherApiDemo.Server.Weather;

/// <summary>A forecast data point normalised across all providers.</summary>
public sealed record WeatherReading(
    string Source,
    DateTimeOffset Timestamp,
    WeatherCategory Category,
    string Description,
    double TemperatureCelsius,
    double? HumidityPercent,
    double? WindSpeedMetersPerSecond,
    double? PrecipitationMillimeters);

public enum WeatherCategory
{
    Unknown,
    Clear,
    Clouds,
    Fog,
    Drizzle,
    Rain,
    Snow,
    Thunderstorm
}
