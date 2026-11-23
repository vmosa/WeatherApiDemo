namespace WeatherApiDemo.Server.Weather;

/// <summary>An external weather API. Implement and register one to add a new source.</summary>
public interface IWeatherProvider
{
    string Name { get; }

    Task<IReadOnlyList<WeatherReading>> GetForecastAsync(Coordinates location, CancellationToken cancellationToken);
}
