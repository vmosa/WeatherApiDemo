namespace WeatherApiDemo.Server.Security;

public static class FeatureFlags
{
    /// <summary>When enabled, the API requires a valid JWT bearer token.</summary>
    public const string JwtAuthentication = "JwtAuthentication";
}
