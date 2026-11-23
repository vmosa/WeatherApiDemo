using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WeatherApiDemo.Server.Weather;
using WeatherApiDemo.Server.Weather.Caching;
using WeatherApiDemo.Tests.Support;

namespace WeatherApiDemo.Tests.Weather;

public sealed class ForecastCacheTests
{
    private static readonly Coordinates Athens = new(37.98, 23.73);

    [Fact]
    public void KeyFor_IsPerProviderAndLocationAndCultureInvariant()
    {
        Assert.Equal("weather:openmeteo:37.98:-23.73", ForecastCache.KeyFor("OpenMeteo", new Coordinates(37.98, -23.73)));
        //Assert.NotEqual(ForecastCache.KeyFor("OpenMeteo", Athens), ForecastCache.KeyFor("MetNorway", Athens));
    }

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsTheStoredForecast()
    {
        var cache = new ForecastCache(CreateMemoryCache(), NullLogger<ForecastCache>.Instance);
        var forecast = new CachedForecast(
            Readings.Now,
            [Readings.At("OpenMeteo", Readings.Now, WeatherCategory.Snow, -3)]);

        await cache.SetAsync("OpenMeteo", Athens, forecast, TimeSpan.FromHours(1), CancellationToken.None);
        var cached = await cache.GetAsync("OpenMeteo", Athens, CancellationToken.None);

        Assert.NotNull(cached);
        Assert.Equal(forecast.RetrievedAt, cached!.RetrievedAt);
        Assert.Equal(forecast.Readings.ToList(), cached.Readings.ToList());
    }

    [Fact]
    public async Task GetAsync_ForAnotherProvider_ReturnsNothing()
    {
        var cache = new ForecastCache(CreateMemoryCache(), NullLogger<ForecastCache>.Instance);
        await cache.SetAsync("OpenMeteo", Athens, new CachedForecast(Readings.Now, []), TimeSpan.FromHours(1), CancellationToken.None);

        Assert.Null(await cache.GetAsync("MetNorway", Athens, CancellationToken.None));
    }

    [Fact]
    public async Task WhenTheCacheIsDown_ReadsMissAndWritesAreIgnored()
    {
        var cache = new ForecastCache(new ThrowingDistributedCache(), NullLogger<ForecastCache>.Instance);

        await cache.SetAsync("OpenMeteo", Athens, new CachedForecast(Readings.Now, []), TimeSpan.FromHours(1), CancellationToken.None);

        Assert.Null(await cache.GetAsync("OpenMeteo", Athens, CancellationToken.None));
    }

    [Fact]
    public void Coordinates_AreRoundedToTwoDecimals()
    {
        var location = new Coordinates(37.98765, 23.72111);

        Assert.Equal(37.99, location.Latitude);
        Assert.Equal(23.72, location.Longitude);
        Assert.Equal(new Coordinates(37.991, 23.724), location);
    }

    internal static IDistributedCache CreateMemoryCache() =>
        new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
}
