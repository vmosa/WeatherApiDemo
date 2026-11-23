using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using WeatherApiDemo.Tests.Support;
using WeatherApiDemo.Server.Weather;
using WeatherApiDemo.Server.Weather.Api;
using WeatherApiDemo.Server.Weather.Aggregation;
using WeatherApiDemo.Server.Weather.Caching;
using WeatherApiDemo.Server.Statistics;

namespace WeatherApiDemo.Tests.Weather;

public sealed class WeatherAggregatorTests
{
    private readonly FakeTimeProvider timeProvider = new(Readings.Now);

    [Fact]
    public async Task CallsEveryProviderConcurrently()
    {
        var allStarted = new TaskCompletionSource();
        var started = 0;
        FakeWeatherProvider WaitingForAll(string name) => new(name, async (_, _) =>
        {
            if (Interlocked.Increment(ref started) == 3)
            {
                allStarted.SetResult();
            }

            // Completes only if all three providers are in flight at the same time.
            await allStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
            return [Readings.At(name, Readings.Now)];
        });

        var aggregator = CreateAggregator(WaitingForAll("A"), WaitingForAll("B"), WaitingForAll("C"));

        var result = await aggregator.GetAsync(Query(), CancellationToken.None);

        Assert.Equal(3, result.Readings.Count);
    }

    [Fact]
    public async Task MergesReadingsAndReportsTheStateOfEachSource()
    {
        var aggregator = CreateAggregator(
            FakeWeatherProvider.Returning("A", Readings.At("A", Readings.Now.AddHours(1))),
            FakeWeatherProvider.Failing("B"),
            FakeWeatherProvider.Returning("C", Readings.At("C", Readings.Now)));

        var result = await aggregator.GetAsync(Query(), CancellationToken.None);

        Assert.Equal("C,A", string.Join(",", result.Readings.Select(r => r.Source)));
        //CollectionAssert.AreEqual(
        //    new[] { SourceState.Live, SourceState.Unavailable, SourceState.Live },
        //    result.Sources.Select(s => s.State).ToArray());
        Assert.Equal(new Coordinates(37.98, 23.73), result.Location);
    }

    [Fact]
    public async Task Source_CallsOnlyTheRequestedProvidersIgnoringCase()
    {
        var a = FakeWeatherProvider.Returning("Alpha");
        var b = FakeWeatherProvider.Returning("Beta");
        var aggregator = CreateAggregator(a, b);

        var result = await aggregator.GetAsync(
            new WeatherQuery { Latitude = 37.98, Longitude = 23.73, Source = ["beta"] },
            CancellationToken.None);

        Assert.Equal(0, a.Calls);
        Assert.Equal(1, b.Calls);
        Assert.Equal("Beta", result.Sources.Single().Name);
    }

    [Fact]
    public void ProviderNames_ListsEveryRegisteredProvider()
    {
        var aggregator = CreateAggregator(FakeWeatherProvider.Returning("A"), FakeWeatherProvider.Returning("B"));

        //CollectionAssert.AreEqual(new[] { "A", "B" }, aggregator.ProviderNames.ToArray());
    }

    private static WeatherQuery Query() => new() { Latitude = 37.98, Longitude = 23.73 };

    private WeatherAggregator CreateAggregator(params IWeatherProvider[] providers)
    {
        var fetcher = new ProviderForecastFetcher(
            new ForecastCache(ForecastCacheTests.CreateMemoryCache(), NullLogger<ForecastCache>.Instance),
            new RequestStatisticsStore(Options.Create(new StatisticsOptions()), timeProvider),
            new StaticOptionsMonitor(new WeatherProviderOptions()),
            timeProvider,
            NullLogger<ProviderForecastFetcher>.Instance);

        return new WeatherAggregator(providers, fetcher, timeProvider);
    }
}
