using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using WeatherApiDemo.Server.Weather;

namespace WeatherApiDemo.Server.Weather.Caching;

public sealed record CachedForecast(DateTimeOffset RetrievedAt, IReadOnlyList<WeatherReading> Readings);

/// <summary>
/// One entry per provider and location, so providers are cached and read independently and in parallel.
/// Cache failures are logged and treated as misses: the cache must never take the service down.
/// </summary>
public sealed class ForecastCache(IDistributedCache cache, ILogger<ForecastCache> logger)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static string KeyFor(string providerName, Coordinates location) =>
        $"weather:{providerName.ToLowerInvariant()}:{location.LatitudeText}:{location.LongitudeText}";

    public async Task<CachedForecast?> GetAsync(
        string providerName,
        Coordinates location,
        CancellationToken cancellationToken)
    {
        var key = KeyFor(providerName, location);
        try
        {
            var payload = await cache.GetAsync(key, cancellationToken);
            return payload is null ? null : JsonSerializer.Deserialize<CachedForecast>(payload, SerializerOptions);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Reading cache entry {CacheKey} failed", key);
            return null;
        }
    }

    public async Task SetAsync(
        string providerName,
        Coordinates location,
        CachedForecast forecast,
        TimeSpan retention,
        CancellationToken cancellationToken)
    {
        var key = KeyFor(providerName, location);
        try
        {
            var payload = JsonSerializer.SerializeToUtf8Bytes(forecast, SerializerOptions);
            var entryOptions = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = retention };
            await cache.SetAsync(key, payload, entryOptions, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Writing cache entry {CacheKey} failed", key);
        }
    }
}
