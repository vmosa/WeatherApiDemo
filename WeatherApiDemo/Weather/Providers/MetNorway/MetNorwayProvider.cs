namespace WeatherApiDemo.Server.Weather.Providers.MetNorway;

/// <summary>Locationforecast 2.0 from https://api.met.no/weatherapi/locationforecast/2.0/documentation.</summary>
public sealed class MetNorwayProvider(IHttpClientFactory httpClientFactory) : IWeatherProvider
{
    public const string ProviderName = "MetNorway";

    public string Name => ProviderName;

    public async Task<IReadOnlyList<WeatherReading>> GetForecastAsync(
        Coordinates location,
        CancellationToken cancellationToken)
    {
        var requestUri =
            $"weatherapi/locationforecast/2.0/compact?lat={location.LatitudeText}&lon={location.LongitudeText}";

        var httpClient = httpClientFactory.CreateClient(ProviderName);
        var response = await httpClient.GetFromJsonAsync<MetNorwayResponse>(requestUri, cancellationToken);

        return response?.Properties.Timeseries.Select(ToReading).ToList() ?? [];
    }

    private static WeatherReading ToReading(MetNorwayTimeseries entry)
    {
        var instant = entry.Data.Instant.Details;
        var period = entry.Data.Next1Hours ?? entry.Data.Next6Hours ?? entry.Data.Next12Hours;
        var symbol = StripTimeOfDay(period?.Summary?.SymbolCode);

        return new WeatherReading(
            ProviderName,
            entry.Time,
            ToCategory(symbol),
            symbol,
            instant.AirTemperature,
            instant.RelativeHumidity,
            instant.WindSpeed,
            period?.Details?.PrecipitationAmount);
    }

    private static string StripTimeOfDay(string? symbolCode)
    {
        if (string.IsNullOrEmpty(symbolCode))
        {
            return string.Empty;
        }

        var separator = symbolCode.IndexOf('_');
        return separator < 0 ? symbolCode : symbolCode[..separator];
    }

    // Symbol codes: https://api.met.no/weatherapi/weathericon/2.0/documentation
    private static WeatherCategory ToCategory(string symbol) => symbol switch
    {
        _ when symbol.Contains("thunder", StringComparison.Ordinal) => WeatherCategory.Thunderstorm,
        _ when symbol.Contains("snow", StringComparison.Ordinal) => WeatherCategory.Snow,
        _ when symbol.Contains("sleet", StringComparison.Ordinal) => WeatherCategory.Snow,
        _ when symbol.Contains("rain", StringComparison.Ordinal) => WeatherCategory.Rain,
        "fog" => WeatherCategory.Fog,
        "clearsky" or "fair" => WeatherCategory.Clear,
        "cloudy" or "partlycloudy" => WeatherCategory.Clouds,
        _ => WeatherCategory.Unknown
    };
}
