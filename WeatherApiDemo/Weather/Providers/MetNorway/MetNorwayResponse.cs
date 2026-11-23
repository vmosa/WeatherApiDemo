using System.Text.Json.Serialization;

namespace WeatherApiDemo.Server.Weather.Providers.MetNorway;

internal sealed record MetNorwayResponse(MetNorwayProperties Properties);

internal sealed record MetNorwayProperties(IReadOnlyList<MetNorwayTimeseries> Timeseries);

internal sealed record MetNorwayTimeseries(DateTimeOffset Time, MetNorwayData Data);

internal sealed record MetNorwayData(
    MetNorwayInstant Instant,
    [property: JsonPropertyName("next_1_hours")] MetNorwayPeriod? Next1Hours,
    [property: JsonPropertyName("next_6_hours")] MetNorwayPeriod? Next6Hours,
    [property: JsonPropertyName("next_12_hours")] MetNorwayPeriod? Next12Hours);

internal sealed record MetNorwayInstant(MetNorwayInstantDetails Details);

internal sealed record MetNorwayInstantDetails(
    [property: JsonPropertyName("air_temperature")] double AirTemperature,
    [property: JsonPropertyName("relative_humidity")] double? RelativeHumidity,
    [property: JsonPropertyName("wind_speed")] double? WindSpeed);

internal sealed record MetNorwayPeriod(MetNorwaySummary? Summary, MetNorwayPeriodDetails? Details);

internal sealed record MetNorwaySummary([property: JsonPropertyName("symbol_code")] string? SymbolCode);

internal sealed record MetNorwayPeriodDetails(
    [property: JsonPropertyName("precipitation_amount")] double? PrecipitationAmount);
