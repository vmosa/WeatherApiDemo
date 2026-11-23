using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using WeatherApiDemo.Server.Statistics;
using WeatherApiDemo.Tests.Support;

namespace WeatherApiDemo.Tests.Statistics;

public sealed class RequestStatisticsStoreTests
{
    private readonly FakeTimeProvider timeProvider = new(Readings.Now);

    [Fact]
    public void ForAProviderWithoutRequests_ReportsZeroes()
    {
        var stats = CreateStore().GetStatistics("OpenMeteo");

        Assert.Equal(0, stats.TotalRequests);
        Assert.Null(stats.AverageResponseTimeMs);
        Assert.True(stats.Buckets.All(bucket => bucket is { Requests: 0, AverageResponseTimeMs: null }));
    }

    [Theory]
    [InlineData(99, PerformanceBucket.Fast)]
    [InlineData(100, PerformanceBucket.Average)]
    [InlineData(200, PerformanceBucket.Average)]
    [InlineData(201, PerformanceBucket.Slow)]
    public void GroupsRequestsIntoBucketsByResponseTime(int milliseconds, PerformanceBucket expected)
    {
        var store = CreateStore();

        store.Record("OpenMeteo", TimeSpan.FromMilliseconds(milliseconds), succeeded: true);

        var bucket = store.GetStatistics("OpenMeteo").Buckets.Single(b => b.Requests == 1);
        Assert.Equal(expected, bucket.Bucket);
    }

    [Fact]
    public void ReportsTotalsAndAveragesOverallAndPerBucket()
    {
        var store = CreateStore();
        store.Record("OpenMeteo", TimeSpan.FromMilliseconds(40), succeeded: true);
        store.Record("OpenMeteo", TimeSpan.FromMilliseconds(60), succeeded: true);
        store.Record("OpenMeteo", TimeSpan.FromMilliseconds(500), succeeded: false);

        var stats = store.GetStatistics("OpenMeteo");

        Assert.Equal(3, stats.TotalRequests);
        Assert.Equal(1, stats.FailedRequests);
        Assert.Equal(200, stats.AverageResponseTimeMs);
        var fast = stats.Buckets.Single(b => b.Bucket == PerformanceBucket.Fast);
        Assert.Equal(2, fast.Requests);
        Assert.Equal(50, fast.AverageResponseTimeMs);
        Assert.Equal("< 100 ms", fast.Range);
        Assert.Equal(500, stats.Buckets.Single(b => b.Bucket == PerformanceBucket.Slow).AverageResponseTimeMs);
    }

    [Fact]
    public void KeepsProvidersApart()
    {
        var store = CreateStore();
        store.Record("OpenMeteo", TimeSpan.FromMilliseconds(10), succeeded: true);

        Assert.Equal(0, store.GetStatistics("MetNorway").TotalRequests);
    }

    [Fact]
    public void RecentStatistics_CoverOnlyTheConfiguredWindow()
    {
        var store = CreateStore();
        store.Record("OpenMeteo", TimeSpan.FromMilliseconds(100), succeeded: true);
        timeProvider.Advance(TimeSpan.FromMinutes(6));
        store.Record("OpenMeteo", TimeSpan.FromMilliseconds(300), succeeded: true);

        var stats = store.GetStatistics("OpenMeteo");

        Assert.Equal(2, stats.TotalRequests);
        Assert.Equal(200, stats.AverageResponseTimeMs);
        Assert.Equal(1, stats.Recent.Requests);
        Assert.Equal(300, stats.Recent.AverageResponseTimeMs);
        Assert.Equal(TimeSpan.FromMinutes(5), stats.Recent.Window);
    }

    [Fact]
    public void RecentStatistics_ExpireEvenWithoutNewRequests()
    {
        var store = CreateStore();
        store.Record("OpenMeteo", TimeSpan.FromMilliseconds(100), succeeded: true);

        timeProvider.Advance(TimeSpan.FromMinutes(5) + TimeSpan.FromTicks(1));

        Assert.Equal(0, store.GetStatistics("OpenMeteo").Recent.Requests);
    }

    [Fact]
    public async Task ConcurrentRecording_LosesNoRequests()
    {
        var store = CreateStore();
        const int perProvider = 5_000;
        string[] providers = ["A", "B", "C"];

        await Task.WhenAll(providers.SelectMany(provider => Enumerable.Range(0, 4).Select(worker => Task.Run(() =>
        {
            for (var i = 0; i < perProvider / 4; i++)
            {
                store.Record(provider, TimeSpan.FromMilliseconds(i % 300), succeeded: i % 10 != 0);
                _ = store.GetStatistics(provider);
            }
        }))));

        foreach (var provider in providers)
        {
            var stats = store.GetStatistics(provider);
            Assert.Equal(perProvider, stats.TotalRequests);
            Assert.Equal(perProvider, stats.Buckets.Sum(b => b.Requests));
            Assert.Equal(perProvider, stats.Recent.Requests);
            Assert.Equal(perProvider / 10, stats.FailedRequests);
        }
    }

    private RequestStatisticsStore CreateStore() =>
        new(Options.Create(new StatisticsOptions()), timeProvider);
}
