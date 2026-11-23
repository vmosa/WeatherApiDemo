using WeatherApiDemo.Server.Weather;
using WeatherApiDemo.Server.Weather.Api;

namespace WeatherApiDemo.Server.Weather.Aggregation;

public static class WeatherReadingFilter
{
    public static IReadOnlyList<WeatherReading> Apply(
        IEnumerable<WeatherReading> readings,
        WeatherQuery query,
        DateTimeOffset now)
    {
        var filtered = readings
            .Where(reading => query.From is null || reading.Timestamp >= query.From)
            .Where(reading => query.To is null || reading.Timestamp <= query.To)
            .Where(reading => query.Category is not { Length: > 0 }
                || query.Category.Any(category => category.Value == reading.Category));

        var descending = query.SortOrder?.Value is SortOrder.Descending;
        var relevanceReference = query.From ?? now;

        var sorted = query.SortBy?.Value switch
        {
            SortField.Relevance => Order(filtered, r => (r.Timestamp - relevanceReference).Duration(), descending),
            SortField.Temperature => Order(filtered, r => r.TemperatureCelsius, descending),
            SortField.Category => Order(filtered, r => r.Category.ToString(), descending, StringComparer.Ordinal),
            SortField.Source => Order(filtered, r => r.Source, descending, StringComparer.Ordinal),
            _ => Order(filtered, r => r.Timestamp, descending)
        };

        return sorted
            .ThenBy(reading => reading.Timestamp)
            .ThenBy(reading => reading.Source, StringComparer.Ordinal)
            .ToList();
    }

    private static IOrderedEnumerable<WeatherReading> Order<TKey>(
        IEnumerable<WeatherReading> readings,
        Func<WeatherReading, TKey> key,
        bool descending,
        IComparer<TKey>? comparer = null) =>
        descending ? readings.OrderByDescending(key, comparer) : readings.OrderBy(key, comparer);
}
