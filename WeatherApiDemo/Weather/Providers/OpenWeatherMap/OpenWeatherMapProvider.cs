using Microsoft.Extensions.Options;

namespace WeatherApiDemo.Server.Weather.Providers.OpenWeatherMap;

/// <summary>5 day / 3 hour forecast from https://openweathermap.org/forecast5.</summary>
public sealed class OpenWeatherMapProvider(IHttpClientFactory httpClientFactory, IOptionsMonitor<WeatherProviderOptions> options)
    : IWeatherProvider
{
    public const string ProviderName = "OpenWeatherMap";

    public string Name => ProviderName;

    public async Task<IReadOnlyList<WeatherReading>> GetForecastAsync(
        Coordinates location,
        CancellationToken cancellationToken)
    {
        var apiKey = options.Get(ProviderName).ApiKey;
        var requestUri =
            $"data/2.5/forecast?lat={location.LatitudeText}&lon={location.LongitudeText}&units=metric&appid={apiKey}";

        var httpClient = httpClientFactory.CreateClient(ProviderName);
        var response = await httpClient.GetFromJsonAsync<OpenWeatherMapResponse>(requestUri, cancellationToken);

        return response?.List.Select(ToReading).ToList() ?? [];
    }

    private static WeatherReading ToReading(OpenWeatherMapEntry entry)
    {
        var condition = entry.Weather.FirstOrDefault();
        var precipitation = (entry.Rain?.ThreeHours ?? 0) + (entry.Snow?.ThreeHours ?? 0);

        return new WeatherReading(
            ProviderName,
            DateTimeOffset.FromUnixTimeSeconds(entry.Dt),
            ToCategory(condition?.Id),
            condition?.Description ?? string.Empty,
            entry.Main.Temp,
            entry.Main.Humidity,
            entry.Wind?.Speed,
            precipitation);
    }

    // Condition codes: https://openweathermap.org/weather-conditions
    private static WeatherCategory ToCategory(int? conditionId) => conditionId switch
    {
        >= 200 and < 300 => WeatherCategory.Thunderstorm,
        >= 300 and < 400 => WeatherCategory.Drizzle,
        >= 500 and < 600 => WeatherCategory.Rain,
        >= 600 and < 700 => WeatherCategory.Snow,
        >= 700 and < 800 => WeatherCategory.Fog,
        800 => WeatherCategory.Clear,
        > 800 and < 900 => WeatherCategory.Clouds,
        _ => WeatherCategory.Unknown
    };
}
