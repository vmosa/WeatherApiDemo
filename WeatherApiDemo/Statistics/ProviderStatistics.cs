namespace WeatherApiDemo.Server.Statistics;

public sealed record ProviderStatistics(
    string Name,
    long TotalRequests,
    long FailedRequests,
    double? AverageResponseTimeMs,
    RecentStatistics Recent,
    IReadOnlyList<BucketStatistics> Buckets);

public sealed record RecentStatistics(TimeSpan Window, long Requests, double? AverageResponseTimeMs);

public sealed record BucketStatistics(
    PerformanceBucket Bucket,
    string Range,
    long Requests,
    double? AverageResponseTimeMs);

public enum PerformanceBucket
{
    Fast,
    Average,
    Slow
}
