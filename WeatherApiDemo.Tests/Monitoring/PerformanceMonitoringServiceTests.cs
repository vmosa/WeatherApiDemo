using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using WeatherApiDemo.Server.Monitoring;
using WeatherApiDemo.Server.Statistics;
using WeatherApiDemo.Tests.Support;

namespace WeatherApiDemo.Tests.Monitoring;

public sealed class PerformanceMonitoringServiceTests
{
    [Fact]
    public async Task AnalysesStatisticsOnTheConfiguredInterval()
    {
        var timeProvider = new FakeTimeProvider(Readings.Now);
        var statistics = new RequestStatisticsStore(
            Options.Create(new StatisticsOptions { RecentWindow = TimeSpan.FromHours(1) }),
            timeProvider);
        var detectorLogger = new FakeLogger<PerformanceAnomalyDetector>();
        var options = Options.Create(new PerformanceMonitoringOptions { Interval = TimeSpan.FromMinutes(1) });
        var detector = new PerformanceAnomalyDetector(
            [FakeWeatherProvider.Returning("OpenMeteo")],
            statistics,
            options,
            detectorLogger);
        using var service = new PerformanceMonitoringService(
            detector,
            options,
            timeProvider,
            NullLogger<PerformanceMonitoringService>.Instance);

        for (var i = 0; i < 10; i++)
        {
            statistics.Record("OpenMeteo", TimeSpan.FromMilliseconds(100), succeeded: true);
        }

        timeProvider.Advance(TimeSpan.FromHours(2));
        for (var i = 0; i < 3; i++)
        {
            statistics.Record("OpenMeteo", TimeSpan.FromMilliseconds(500), succeeded: true);
        }

        await service.StartAsync(CancellationToken.None);
        Assert.Equal(0, detectorLogger.Collector.Count);

        // ExecuteAsync starts on a background thread, so keep ticking until its timer has fired.
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (detectorLogger.Collector.Count == 0 && DateTime.UtcNow < deadline)
        {
            timeProvider.Advance(options.Value.Interval);
            await Task.Delay(10);
        }

        await service.StopAsync(CancellationToken.None);
        StringAssert.Contains(detectorLogger.LatestRecord.Message, "Performance anomaly for OpenMeteo");
    }
}
