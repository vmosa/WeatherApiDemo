using Microsoft.Extensions.Options;
using WeatherApiDemo.Server.Statistics;
using WeatherApiDemo.Server.Weather;

namespace WeatherApiDemo.Server.Monitoring;

public sealed record PerformanceAnomaly(
    string Provider,
    double RecentAverageMs,
    double OverallAverageMs,
    long RecentRequests,
    TimeSpan Window);

/// <summary>Flags providers whose recent average response time exceeds their overall average by the threshold.</summary>
public sealed class PerformanceAnomalyDetector(
    IEnumerable<IWeatherProvider> providers,
    RequestStatisticsStore statistics,
    IOptions<PerformanceMonitoringOptions> options,
    ILogger<PerformanceAnomalyDetector> logger)
{
    private readonly PerformanceMonitoringOptions options = options.Value;

    public IReadOnlyList<PerformanceAnomaly> Analyze()
    {
        var anomalies = providers
            .Select(provider => statistics.GetStatistics(provider.Name))
            .Where(IsAnomalous)
            .Select(stats => new PerformanceAnomaly(
                stats.Name,
                stats.Recent.AverageResponseTimeMs!.Value,
                stats.AverageResponseTimeMs!.Value,
                stats.Recent.Requests,
                stats.Recent.Window))
            .ToList();

        foreach (var anomaly in anomalies)
        {
            logger.LogWarning(
                "Performance anomaly for {Provider}: average {RecentAverageMs} ms over the last {Window} " +
                "({RecentRequests} requests) against an overall average of {OverallAverageMs} ms",
                anomaly.Provider,
                anomaly.RecentAverageMs,
                anomaly.Window,
                anomaly.RecentRequests,
                anomaly.OverallAverageMs);
        }

        return anomalies;
    }

    private bool IsAnomalous(ProviderStatistics stats) =>
        stats is { AverageResponseTimeMs: { } overall, Recent.AverageResponseTimeMs: { } recent }
        && stats.Recent.Requests >= options.MinimumRecentRequests
        && recent > overall * (1 + options.DegradationThreshold);
}
