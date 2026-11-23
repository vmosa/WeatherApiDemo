using System.Text.Json.Serialization;

namespace WeatherApiDemo.Server.Weather.Providers.OpenWeatherMap;

internal sealed record OpenWeatherMapResponse(IReadOnlyList<OpenWeatherMapEntry> List);

internal sealed record OpenWeatherMapEntry(
    long Dt,
    OpenWeatherMapMain Main,
    IReadOnlyList<OpenWeatherMapCondition> Weather,
    OpenWeatherMapWind? Wind,
    OpenWeatherMapPrecipitation? Rain,
    OpenWeatherMapPrecipitation? Snow);

internal sealed record OpenWeatherMapMain(double Temp, double? Humidity);

internal sealed record OpenWeatherMapCondition(int Id, string Description);

internal sealed record OpenWeatherMapWind(double? Speed);

internal sealed record OpenWeatherMapPrecipitation([property: JsonPropertyName("3h")] double? ThreeHours);
