using System.ComponentModel.DataAnnotations;

namespace WeatherApiDemo.Server.Security;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Requests each caller may make per window.</summary>
    [Range(1, int.MaxValue)]
    public int PermitLimit { get; set; } = 60;

    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
}
