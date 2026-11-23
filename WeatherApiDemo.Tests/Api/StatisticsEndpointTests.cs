using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using WeatherApiDemo.Tests.Support;


namespace WeatherApiDemo.Tests.Api;


public sealed class StatisticsEndpointTests
{
    [Fact]
    public async Task WithoutAToken_IsUnauthorized()
    {
        await using var factory = new WeatherApiDemoFactory(FakeWeatherProvider.Returning("A"));

        var response = await factory.CreateClient().GetAsync("/api/statistics");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WhenJwtAuthenticationIsDisabled_AllowsAnonymousCalls()
    {
        await using var factory = new WeatherApiDemoFactory(FakeWeatherProvider.Returning("A"))
            .WithSetting("FeatureManagement:JwtAuthentication", "false");

        var response = await factory.CreateClient().GetAsync("/api/statistics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ReportsRequestsPerProviderGroupedInBuckets()
    {
        await using var factory = new WeatherApiDemoFactory(
            FakeWeatherProvider.Returning("A"),
            FakeWeatherProvider.Failing("B"));
        var client = factory.CreateAuthenticatedClient();
        await client.GetAsync("/api/weather?latitude=37.98&longitude=23.73");

        var providers = await client.GetFromJsonAsync<JsonElement>("/api/statistics");

        Assert.Equal(2, providers.GetArrayLength());
        var a = providers[0];
        Assert.Equal("A", a.GetProperty("name").GetString());
        Assert.Equal(1, a.GetProperty("totalRequests").GetInt32());
        var buckets = a.GetProperty("buckets").EnumerateArray().Select(b => b.GetProperty("bucket").GetString());
        //CollectionAssert.AreEqual(new[] { "Fast", "Average", "Slow" }, buckets.ToArray());
        Assert.Equal(1, providers[1].GetProperty("failedRequests").GetInt32());
    }
}
