using WeatherApiDemo.Server.Weather;
using WeatherApiDemo.Server.Weather.Api;

namespace WeatherApiDemo.Server.Weather.Aggregation;

public sealed record AggregatedWeather(
    Coordinates Location,
    IReadOnlyList<SourceStatus> Sources,
    IReadOnlyList<WeatherReading> Readings);

public sealed record SourceStatus(string Name, SourceState State, DateTimeOffset? RetrievedAt);

public sealed class WeatherAggregator(
    IEnumerable<IWeatherProvider> providers,
    ProviderForecastFetcher fetcher,
    TimeProvider timeProvider)
{
    public IReadOnlyList<string> ProviderNames { get; } = providers.Select(provider => provider.Name).ToList();

    public async Task<AggregatedWeather> GetAsync(WeatherQuery query, CancellationToken cancellationToken)
    {
        var location = new Coordinates(query.Latitude??0, query.Longitude??0);

        var forecasts = await Task.WhenAll(providers
            .Where(provider => IsRequested(provider, query))
            .Select(provider => fetcher.FetchAsync(provider, location, cancellationToken)));

        var sources = forecasts
            .Select(forecast => new SourceStatus(forecast.Source, forecast.State, forecast.RetrievedAt))
            .ToList();
        var readings = WeatherReadingFilter.Apply(
            forecasts.SelectMany(forecast => forecast.Readings),
            query,
            timeProvider.GetUtcNow());

        return new AggregatedWeather(location, sources, readings);
    }

    private static bool IsRequested(IWeatherProvider provider, WeatherQuery query) =>
        query.Source is not { Length: > 0 }
        || query.Source.Contains(provider.Name, StringComparer.OrdinalIgnoreCase);
}
