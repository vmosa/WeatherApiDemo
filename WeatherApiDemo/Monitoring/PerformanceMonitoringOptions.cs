using System.ComponentModel.DataAnnotations;

namespace WeatherApiDemo.Server.Monitoring;

public sealed class PerformanceMonitoringOptions
{
    public const string SectionName = "PerformanceMonitoring";

    public bool Enabled { get; set; } = true;

    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Relative increase of the recent over the overall average that counts as an anomaly; 0.5 is 50%.</summary>
    [Range(0d, double.MaxValue)]
    public double DegradationThreshold { get; set; } = 0.5;

    /// <summary>Fewer recent requests than this are too few to judge.</summary>
    [Range(1, int.MaxValue)]
    public int MinimumRecentRequests { get; set; } = 3;
}
