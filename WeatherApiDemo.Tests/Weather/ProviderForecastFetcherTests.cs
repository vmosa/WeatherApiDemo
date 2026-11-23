using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using WeatherApiDemo.Server.Statistics;
using WeatherApiDemo.Server.Weather;
using WeatherApiDemo.Server.Weather.Aggregation;
using WeatherApiDemo.Server.Weather.Caching;
using WeatherApiDemo.Tests.Support;


namespace WeatherApiDemo.Tests.Weather;

public sealed class ProviderForecastFetcherTests
{
    private static readonly Coordinates Athens = new(37.98, 23.73);
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    private readonly FakeTimeProvider timeProvider = new(Readings.Now);
    private ForecastCache cache = null!;
    private RequestStatisticsStore statistics = null!;

    public ProviderForecastFetcherTests()
    {
        cache = new ForecastCache(ForecastCacheTests.CreateMemoryCache(), NullLogger<ForecastCache>.Instance);
        statistics = new RequestStatisticsStore(Options.Create(new StatisticsOptions()), timeProvider);
    }

    [Fact]
    public async Task WithoutACachedForecast_FetchesLiveAndCachesIt()
    {
        var reading = Readings.At("OpenMeteo", Readings.Now);
        var provider = FakeWeatherProvider.Returning("OpenMeteo", reading);

        var forecast = await CreateFetcher().FetchAsync(provider, Athens, CancellationToken.None);

        Assert.Equal(SourceState.Live, forecast.State);
        Assert.Equal(Readings.Now, forecast.RetrievedAt);
        Assert.Equal(new[] { reading }, forecast.Readings.ToArray());
        Assert.NotNull(await cache.GetAsync("OpenMeteo", Athens, CancellationToken.None));
    }

    [Fact]
    public async Task WithAFreshCachedForecast_ServesItWithoutCallingTheProvider()
    {
        await SeedCache("OpenMeteo", retrievedAt: Readings.Now.AddMinutes(-9));
        var provider = FakeWeatherProvider.Returning("OpenMeteo");

        var forecast = await CreateFetcher().FetchAsync(provider, Athens, CancellationToken.None);

        Assert.Equal(SourceState.Cached, forecast.State);
        Assert.Equal(0, provider.Calls);
        Assert.NotEmpty(forecast.Readings);
        Assert.Single(forecast.Readings);
    }

    [Fact]
    public async Task WithAnExpiredCachedForecast_FetchesLive()
    {
        await SeedCache("OpenMeteo", retrievedAt: Readings.Now - CacheDuration);
        var provider = FakeWeatherProvider.Returning("OpenMeteo");

        var forecast = await CreateFetcher().FetchAsync(provider, Athens, CancellationToken.None);

        Assert.Equal(SourceState.Live, forecast.State);
        Assert.Equal(1, provider.Calls);
    }

    [Fact]
    public async Task WhenTheProviderFails_FallsBackToTheExpiredCachedForecast()
    {
        var retrievedAt = Readings.Now.AddHours(-2);
        await SeedCache("OpenMeteo", retrievedAt);

        var forecast = await CreateFetcher().FetchAsync(
            FakeWeatherProvider.Failing("OpenMeteo"),
            Athens,
            CancellationToken.None);

        Assert.Equal(SourceState.Fallback, forecast.State);
        Assert.Equal(retrievedAt, forecast.RetrievedAt);
        Assert.Single(forecast.Readings);
    }

    [Fact]
    public async Task WhenTheProviderFailsWithNothingCached_ReportsItUnavailable()
    {
        var forecast = await CreateFetcher().FetchAsync(
            FakeWeatherProvider.Failing("OpenMeteo"),
            Athens,
            CancellationToken.None);

        Assert.Equal(SourceState.Unavailable, forecast.State);
        Assert.Null(forecast.RetrievedAt);
        Assert.Empty(forecast.Readings);
    }

    [Fact]
    public async Task WhenTheCacheIsDown_StillServesLiveData()
    {
        cache = new ForecastCache(new ThrowingDistributedCache(), NullLogger<ForecastCache>.Instance);

        var forecast = await CreateFetcher().FetchAsync(
            FakeWeatherProvider.Returning("OpenMeteo", Readings.At("OpenMeteo", Readings.Now)),
            Athens,
            CancellationToken.None);

        Assert.Equal(SourceState.Live, forecast.State);
        Assert.Single(forecast.Readings);
    }

    [Fact]
    public async Task RecordsTheDurationAndOutcomeOfEveryProviderCall()
    {
        var slowProvider = new FakeWeatherProvider("OpenMeteo", (_, _) =>
        {
            timeProvider.Advance(TimeSpan.FromMilliseconds(150));
            return Task.FromResult<IReadOnlyList<WeatherReading>>([]);
        });
        var fetcher = CreateFetcher();

        await fetcher.FetchAsync(slowProvider, Athens, CancellationToken.None);
        await fetcher.FetchAsync(FakeWeatherProvider.Failing("MetNorway"), Athens, CancellationToken.None);

        var openMeteo = statistics.GetStatistics("OpenMeteo");
        Assert.Equal(1, openMeteo.TotalRequests);
        Assert.Equal(150, openMeteo.AverageResponseTimeMs);
        Assert.Equal(1, openMeteo.Buckets.Single(b => b.Bucket == PerformanceBucket.Average).Requests);
        Assert.Equal(1, statistics.GetStatistics("MetNorway").FailedRequests);
    }

    [Fact]
    public async Task CacheHitsAreNotCountedAsProviderRequests()
    {
        await SeedCache("OpenMeteo", Readings.Now);

        await CreateFetcher().FetchAsync(FakeWeatherProvider.Returning("OpenMeteo"), Athens, CancellationToken.None);

        Assert.Equal(0, statistics.GetStatistics("OpenMeteo").TotalRequests);
    }

    [Fact]
    public async Task WhenTheCallerCancels_PropagatesTheCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        var provider = new FakeWeatherProvider("OpenMeteo", (_, token) =>
        {
            cancellation.Cancel();
            token.ThrowIfCancellationRequested();
            return Task.FromResult<IReadOnlyList<WeatherReading>>([]);
        });

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            CreateFetcher().FetchAsync(provider, Athens, cancellation.Token));
    }

    private ProviderForecastFetcher CreateFetcher() =>
        new(
            cache,
            statistics,
            new StaticOptionsMonitor(new WeatherProviderOptions
            {
                CacheDuration = CacheDuration,
                FallbackDuration = TimeSpan.FromHours(6)
            }),
            timeProvider,
            NullLogger<ProviderForecastFetcher>.Instance);

    private Task SeedCache(string provider, DateTimeOffset retrievedAt) =>
        cache.SetAsync(
            provider,
            Athens,
            new CachedForecast(retrievedAt, [Readings.At(provider, retrievedAt)]),
            TimeSpan.FromHours(6),
            CancellationToken.None);
}
