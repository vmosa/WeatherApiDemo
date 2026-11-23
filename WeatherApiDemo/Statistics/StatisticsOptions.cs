namespace WeatherApiDemo.Server.Statistics;

public sealed class StatisticsOptions
{
    public const string SectionName = "Statistics";

    /// <summary>Responses faster than this are fast.</summary>
    public TimeSpan FastThreshold { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>Responses slower than this are slow; anything in between is average.</summary>
    public TimeSpan SlowThreshold { get; set; } = TimeSpan.FromMilliseconds(200);

    /// <summary>Period covered by the recent statistics used for anomaly detection.</summary>
    public TimeSpan RecentWindow { get; set; } = TimeSpan.FromMinutes(5);
}
