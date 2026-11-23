using Microsoft.Extensions.Options;

namespace WeatherApiDemo.Server.Monitoring;

public sealed class PerformanceMonitoringService(
    PerformanceAnomalyDetector detector,
    IOptions<PerformanceMonitoringOptions> options,
    TimeProvider timeProvider,
    ILogger<PerformanceMonitoringService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.Interval, timeProvider);

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    detector.Analyze();
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Performance analysis failed");
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }
    }
}
