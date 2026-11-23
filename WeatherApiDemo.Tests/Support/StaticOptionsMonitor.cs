using Microsoft.Extensions.Options;
using WeatherApiDemo.Server.Weather;

namespace WeatherApiDemo.Tests.Support;

/// <summary>Returns the same options for every provider name.</summary>
internal sealed class StaticOptionsMonitor(WeatherProviderOptions options) : IOptionsMonitor<WeatherProviderOptions>
{
    public WeatherProviderOptions CurrentValue => options;

    public WeatherProviderOptions Get(string? name) => options;

    public IDisposable? OnChange(Action<WeatherProviderOptions, string?> listener) => null;
}
