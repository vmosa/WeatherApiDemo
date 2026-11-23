using System.ComponentModel.DataAnnotations;

namespace WeatherApiDemo.Server.Weather;

public sealed class WeatherProviderOptions
{
    public const string SectionName = "WeatherProviders";

    public bool Enabled { get; set; } = true;

    [Required]
    public Uri? BaseAddress { get; set; }

    public string? ApiKey { get; set; }

    /// <summary>Some providers, such as MET Norway, reject requests without an identifying User-Agent.</summary>
    public string UserAgent { get; set; } = "PlaygroundApp/1.0";

    /// <summary>How long a cached forecast is served without calling the provider.</summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>How long a forecast is kept as a fallback for when the provider is unavailable.</summary>
    public TimeSpan FallbackDuration { get; set; } = TimeSpan.FromHours(6);
}
