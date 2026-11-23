using WeatherApiDemo.Server.Weather;

namespace WeatherApiDemo.Server.Weather.Aggregation;

public sealed record ProviderForecast(
    string Source,
    SourceState State,
    DateTimeOffset? RetrievedAt,
    IReadOnlyList<WeatherReading> Readings);

public enum SourceState
{
    /// <summary>Fetched from the provider during this request.</summary>
    Live,

    /// <summary>Served from cache within its configured duration.</summary>
    Cached,

    /// <summary>The provider failed; the last known forecast was served instead.</summary>
    Fallback,

    /// <summary>The provider failed and no fallback forecast exists.</summary>
    Unavailable
}
