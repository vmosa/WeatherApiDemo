using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using WeatherApiDemo.Server.Monitoring;
using WeatherApiDemo.Server.Statistics;
using WeatherApiDemo.Tests.Support;

namespace WeatherApiDemo.Tests.Monitoring;


public sealed class PerformanceAnomalyDetectorTests
{
    private readonly FakeTimeProvider timeProvider = new(Readings.Now);
    private readonly FakeLogger<PerformanceAnomalyDetector> logger = new();
    private RequestStatisticsStore statistics = null!;

    public PerformanceAnomalyDetectorTests()
    {
        statistics = new RequestStatisticsStore(Options.Create(new StatisticsOptions()), timeProvider);
    }
        

    [Fact]
    public void WhenTheRecentAverageExceedsTheOverallByMoreThanTheThreshold_ReportsAndLogsAnAnomaly()
    {
        RecordOldRequests("OpenMeteo", count: 10, milliseconds: 100);
        RecordRecentRequests("OpenMeteo", count: 3, milliseconds: 400);

        var anomaly = CreateDetector("OpenMeteo").Analyze().Single();

        Assert.Equal("OpenMeteo", anomaly.Provider);
        Assert.Equal(400, anomaly.RecentAverageMs);
        Assert.Equal(169.23, anomaly.OverallAverageMs);
        Assert.Equal(LogLevel.Warning, logger.LatestRecord.Level);
        Assert.Contains("Performance anomaly for OpenMeteo", logger.LatestRecord.Message);
    }

    [Fact]
    public void WhenTheRecentAverageIsWithinTheThreshold_ReportsNothing()
    {
        RecordOldRequests("OpenMeteo", count: 10, milliseconds: 100);
        RecordRecentRequests("OpenMeteo", count: 3, milliseconds: 130);

        Assert.Empty(CreateDetector("OpenMeteo").Analyze());
        Assert.Equal(0, logger.Collector.Count);
    }

    [Fact]
    public void WithTooFewRecentRequests_ReportsNothing()
    {
        RecordOldRequests("OpenMeteo", count: 10, milliseconds: 100);
        RecordRecentRequests("OpenMeteo", count: 2, milliseconds: 1_000);

        Assert.Empty(CreateDetector("OpenMeteo").Analyze());
    }

    [Fact]
    public void WithoutAnyRequests_ReportsNothing()
    {
        Assert.Empty(CreateDetector("OpenMeteo", "MetNorway").Analyze());
    }

    [Fact]
    public void JudgesEveryProviderIndependently()
    {
        RecordOldRequests("OpenMeteo", count: 10, milliseconds: 100);
        RecordOldRequests("MetNorway", count: 10, milliseconds: 100);
        RecordRecentRequests("OpenMeteo", count: 3, milliseconds: 100);
        RecordRecentRequests("MetNorway", count: 3, milliseconds: 900);

        var anomalies = CreateDetector("OpenMeteo", "MetNorway").Analyze();

        Assert.Equal("MetNorway", anomalies.Single().Provider);
    }

    private void RecordOldRequests(string provider, int count, int milliseconds)
    {
        for (var i = 0; i < count; i++)
        {
            statistics.Record(provider, TimeSpan.FromMilliseconds(milliseconds), succeeded: true);
        }

        timeProvider.Advance(TimeSpan.FromMinutes(10));
    }

    private void RecordRecentRequests(string provider, int count, int milliseconds)
    {
        for (var i = 0; i < count; i++)
        {
            statistics.Record(provider, TimeSpan.FromMilliseconds(milliseconds), succeeded: true);
        }
    }

    private PerformanceAnomalyDetector CreateDetector(params string[] providers) =>
        new(
            providers.Select(name => FakeWeatherProvider.Returning(name)),
            statistics,
            Options.Create(new PerformanceMonitoringOptions()),
            logger);
}
