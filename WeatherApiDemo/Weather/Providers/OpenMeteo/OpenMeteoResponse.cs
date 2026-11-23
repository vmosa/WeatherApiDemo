using System.Text.Json.Serialization;

namespace WeatherApiDemo.Server.Weather.Providers.OpenMeteo;

internal sealed record OpenMeteoResponse(OpenMeteoHourly Hourly);

internal sealed record OpenMeteoHourly(
    IReadOnlyList<long> Time,
    [property: JsonPropertyName("temperature_2m")] IReadOnlyList<double?> Temperature2m,
    [property: JsonPropertyName("relative_humidity_2m")] IReadOnlyList<double?> RelativeHumidity2m,
    [property: JsonPropertyName("precipitation")] IReadOnlyList<double?> Precipitation,
    [property: JsonPropertyName("weather_code")] IReadOnlyList<int?> WeatherCode,
    [property: JsonPropertyName("wind_speed_10m")] IReadOnlyList<double?> WindSpeed10m);
