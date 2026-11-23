using Microsoft.Extensions.Options;
using WeatherApiDemo.Server.Statistics;
using WeatherApiDemo.Server.Weather;
using WeatherApiDemo.Server.Weather.Caching;

namespace WeatherApiDemo.Server.Weather.Aggregation;

/// <summary>Serves a provider's forecast from cache, then live, then from the fallback copy.</summary>
public sealed class ProviderForecastFetcher(
    ForecastCache cache,
    RequestStatisticsStore statistics,
    IOptionsMonitor<WeatherProviderOptions> options,
    TimeProvider timeProvider,
    ILogger<ProviderForecastFetcher> logger)
{
    public async Task<ProviderForecast> FetchAsync(
        IWeatherProvider provider,
        Coordinates location,
        CancellationToken cancellationToken)
    {
        var providerOptions = options.Get(provider.Name);

        // A single entry serves both purposes: it is fresh for CacheDuration and kept for FallbackDuration.
        var cached = await cache.GetAsync(provider.Name, location, cancellationToken);
        if (cached is not null && timeProvider.GetUtcNow() - cached.RetrievedAt < providerOptions.CacheDuration)
        {
            return new ProviderForecast(provider.Name, SourceState.Cached, cached.RetrievedAt, cached.Readings);
        }

        var startedAt = timeProvider.GetTimestamp();
        try
        {
            var readings = await provider.GetForecastAsync(location, cancellationToken);
            statistics.Record(provider.Name, timeProvider.GetElapsedTime(startedAt), succeeded: true);

            var forecast = new CachedForecast(timeProvider.GetUtcNow(), readings);
            await cache.SetAsync(provider.Name, location, forecast, providerOptions.FallbackDuration, cancellationToken);

            return new ProviderForecast(provider.Name, SourceState.Live, forecast.RetrievedAt, readings);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            statistics.Record(provider.Name, timeProvider.GetElapsedTime(startedAt), succeeded: false);
            logger.LogWarning(exception, "Provider {Provider} failed for {Location}", provider.Name, location);

            return cached is null
                ? new ProviderForecast(provider.Name, SourceState.Unavailable, null, [])
                : new ProviderForecast(provider.Name, SourceState.Fallback, cached.RetrievedAt, cached.Readings);
        }
    }
}
