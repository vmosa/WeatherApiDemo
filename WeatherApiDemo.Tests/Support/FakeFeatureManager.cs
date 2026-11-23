using Microsoft.FeatureManagement;

namespace WeatherApiDemo.Tests.Support;

internal sealed class FakeFeatureManager(string feature, bool enabled) : IFeatureManager
{
    public async IAsyncEnumerable<string> GetFeatureNamesAsync()
    {
        await Task.CompletedTask;
        yield return feature;
    }

    public Task<bool> IsEnabledAsync(string name) => Task.FromResult(name == feature && enabled);

    public Task<bool> IsEnabledAsync<TContext>(string name, TContext context) => IsEnabledAsync(name);
}
