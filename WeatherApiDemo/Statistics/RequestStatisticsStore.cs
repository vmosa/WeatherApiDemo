using System.Collections.Concurrent;
using System.Globalization;
using Microsoft.Extensions.Options;

namespace WeatherApiDemo.Server.Statistics;

/// <summary>Thread-safe, in-memory statistics of the calls made to each external API.</summary>
public sealed class RequestStatisticsStore(IOptions<StatisticsOptions> options, TimeProvider timeProvider)
{
    private readonly StatisticsOptions options = options.Value;
    private readonly ConcurrentDictionary<string, ProviderRecorder> recorders = new(StringComparer.Ordinal);

    public void Record(string providerName, TimeSpan duration, bool succeeded)
    {
        var recorder = recorders.GetOrAdd(providerName, _ => new ProviderRecorder());
        recorder.Record(BucketFor(duration), duration, succeeded, timeProvider.GetUtcNow(), options.RecentWindow);
    }

    public ProviderStatistics GetStatistics(string providerName)
    {
        var snapshot = recorders.TryGetValue(providerName, out var recorder)
            ? recorder.Snapshot(timeProvider.GetUtcNow(), options.RecentWindow)
            : ProviderRecorder.Empty;

        return new ProviderStatistics(
            providerName,
            snapshot.Requests,
            snapshot.Failures,
            Average(snapshot.TotalDuration, snapshot.Requests),
            new RecentStatistics(
                options.RecentWindow,
                snapshot.RecentRequests,
                Average(snapshot.RecentDuration, snapshot.RecentRequests)),
            Enum.GetValues<PerformanceBucket>()
                .Select(bucket => new BucketStatistics(
                    bucket,
                    RangeOf(bucket),
                    snapshot.BucketRequests[(int)bucket],
                    Average(snapshot.BucketDurations[(int)bucket], snapshot.BucketRequests[(int)bucket])))
                .ToList());
    }

    private PerformanceBucket BucketFor(TimeSpan duration)
    {
        if (duration < options.FastThreshold)
        {
            return PerformanceBucket.Fast;
        }

        return duration <= options.SlowThreshold ? PerformanceBucket.Average : PerformanceBucket.Slow;
    }

    private string RangeOf(PerformanceBucket bucket)
    {
        var fast = options.FastThreshold.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
        var slow = options.SlowThreshold.TotalMilliseconds.ToString(CultureInfo.InvariantCulture);

        return bucket switch
        {
            PerformanceBucket.Fast => $"< {fast} ms",
            PerformanceBucket.Average => $"{fast}-{slow} ms",
            _ => $"> {slow} ms"
        };
    }

    private static double? Average(TimeSpan total, long count) =>
        count == 0 ? null : Math.Round(total.TotalMilliseconds / count, 2);

    private sealed record Snapshot(
        long Requests,
        long Failures,
        TimeSpan TotalDuration,
        long[] BucketRequests,
        TimeSpan[] BucketDurations,
        long RecentRequests,
        TimeSpan RecentDuration);

    // A lock per provider keeps counters and the recent-sample queue mutually consistent;
    // contention is limited to concurrent calls to the same provider.
    private sealed class ProviderRecorder
    {
        private static readonly int BucketCount = Enum.GetValues<PerformanceBucket>().Length;

        public static readonly Snapshot Empty =
            new(0, 0, TimeSpan.Zero, new long[BucketCount], new TimeSpan[BucketCount], 0, TimeSpan.Zero);

        private readonly Lock gate = new();
        private readonly long[] bucketRequests = new long[BucketCount];
        private readonly TimeSpan[] bucketDurations = new TimeSpan[BucketCount];
        private readonly Queue<(DateTimeOffset At, TimeSpan Duration)> recentSamples = new();
        private long failures;
        private TimeSpan recentDuration;

        public void Record(
            PerformanceBucket bucket,
            TimeSpan duration,
            bool succeeded,
            DateTimeOffset now,
            TimeSpan recentWindow)
        {
            lock (gate)
            {
                bucketRequests[(int)bucket]++;
                bucketDurations[(int)bucket] += duration;
                if (!succeeded)
                {
                    failures++;
                }

                recentSamples.Enqueue((now, duration));
                recentDuration += duration;
                DropSamplesBefore(now - recentWindow);
            }
        }

        public Snapshot Snapshot(DateTimeOffset now, TimeSpan recentWindow)
        {
            lock (gate)
            {
                DropSamplesBefore(now - recentWindow);

                return new Snapshot(
                    bucketRequests.Sum(),
                    failures,
                    bucketDurations.Aggregate(TimeSpan.Zero, (total, duration) => total + duration),
                    (long[])bucketRequests.Clone(),
                    (TimeSpan[])bucketDurations.Clone(),
                    recentSamples.Count,
                    recentDuration);
            }
        }

        private void DropSamplesBefore(DateTimeOffset cutoff)
        {
            while (recentSamples.TryPeek(out var oldest) && oldest.At < cutoff)
            {
                recentDuration -= recentSamples.Dequeue().Duration;
            }
        }
    }
}
