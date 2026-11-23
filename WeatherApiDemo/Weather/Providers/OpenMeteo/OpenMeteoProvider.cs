namespace WeatherApiDemo.Server.Weather.Providers.OpenMeteo;

/// <summary>Hourly forecast from https://open-meteo.com/en/docs.</summary>
public sealed class OpenMeteoProvider(IHttpClientFactory httpClientFactory) : IWeatherProvider
{
    public const string ProviderName = "OpenMeteo";

    private const int ForecastDays = 5;

    public string Name => ProviderName;

    public async Task<IReadOnlyList<WeatherReading>> GetForecastAsync(
        Coordinates location,
        CancellationToken cancellationToken)
    {
        var requestUri =
            $"v1/forecast?latitude={location.LatitudeText}&longitude={location.LongitudeText}" +
            "&hourly=temperature_2m,relative_humidity_2m,precipitation,weather_code,wind_speed_10m" +
            $"&wind_speed_unit=ms&timeformat=unixtime&forecast_days={ForecastDays}";

        var httpClient = httpClientFactory.CreateClient(ProviderName);
        var response = await httpClient.GetFromJsonAsync<OpenMeteoResponse>(requestUri, cancellationToken);

        return response is null ? [] : ToReadings(response.Hourly);
    }

    private static List<WeatherReading> ToReadings(OpenMeteoHourly hourly)
    {
        var readings = new List<WeatherReading>(hourly.Time.Count);

        for (var i = 0; i < hourly.Time.Count; i++)
        {
            if (hourly.Temperature2m[i] is not { } temperature)
            {
                continue;
            }

            var code = hourly.WeatherCode[i];
            readings.Add(new WeatherReading(
                ProviderName,
                DateTimeOffset.FromUnixTimeSeconds(hourly.Time[i]),
                ToCategory(code),
                Describe(code),
                temperature,
                hourly.RelativeHumidity2m[i],
                hourly.WindSpeed10m[i],
                hourly.Precipitation[i]));
        }

        return readings;
    }

    // WMO weather interpretation codes, as listed at https://open-meteo.com/en/docs.
    private static WeatherCategory ToCategory(int? code) => code switch
    {
        0 or 1 => WeatherCategory.Clear,
        2 or 3 => WeatherCategory.Clouds,
        45 or 48 => WeatherCategory.Fog,
        >= 51 and <= 57 => WeatherCategory.Drizzle,
        (>= 61 and <= 67) or (>= 80 and <= 82) => WeatherCategory.Rain,
        (>= 71 and <= 77) or 85 or 86 => WeatherCategory.Snow,
        >= 95 and <= 99 => WeatherCategory.Thunderstorm,
        _ => WeatherCategory.Unknown
    };

    private static string Describe(int? code) => code switch
    {
        0 => "clear sky",
        1 => "mainly clear",
        2 => "partly cloudy",
        3 => "overcast",
        _ => ToCategory(code).ToString().ToLowerInvariant()
    };
}
