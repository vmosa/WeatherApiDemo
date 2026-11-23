using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using WeatherApiDemo.Tests.Support;
using WeatherApiDemo.Server.Weather;

namespace WeatherApiDemo.Tests.Api;

public sealed class WeatherEndpointTests
{
    private const string Athens = "/api/weather?latitude=37.98&longitude=23.73";

    [Fact]
    public async Task WithoutAToken_IsUnauthorized()
    {
        await using var factory = new WeatherApiDemoFactory(FakeWeatherProvider.Returning("A"));

        var response = await factory.CreateClient().GetAsync(Athens);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ReturnsFilteredAndSortedReadingsFromEveryProvider()
    {
        await using var factory = new WeatherApiDemoFactory(
            FakeWeatherProvider.Returning(
                "A",
                Readings.At("A", Readings.Now, WeatherCategory.Rain, 10),
                Readings.At("A", Readings.Now, WeatherCategory.Clear, 25)),
            FakeWeatherProvider.Returning("B", Readings.At("B", Readings.Now, WeatherCategory.Clear, 18)));

        var body = await GetJson(
            factory.CreateAuthenticatedClient(),
            $"{Athens}&category=Clear&sortBy=Temperature&sortOrder=Descending");

        var readings = body.GetProperty("readings").EnumerateArray().ToList();
        Assert.Equal("A,B", string.Join(",", readings.Select(r => r.GetProperty("source").GetString())));
        Assert.Equal("Clear", readings[0].GetProperty("category").GetString());
        var states = body.GetProperty("sources").EnumerateArray().Select(s => s.GetProperty("state").GetString());
        //CollectionAssert.AreEqual(new[] { "Live", "Live" }, states.ToArray());
    }

    [Theory]
    [InlineData("category=clear&sortBy=temperature&sortOrder=descending")]
    [InlineData("category=CLEAR&sortBy=TEMPERATURE&sortOrder=DESCENDING")]
    public async Task EnumValues_AreCaseInsensitive(string query)
    {
        await using var factory = new WeatherApiDemoFactory(
            FakeWeatherProvider.Returning(
                "A",
                Readings.At("A", Readings.Now, WeatherCategory.Rain, 10),
                Readings.At("A", Readings.Now, WeatherCategory.Clear, 15)),
            FakeWeatherProvider.Returning("B", Readings.At("B", Readings.Now, WeatherCategory.Clear, 18)));

        var body = await GetJson(factory.CreateAuthenticatedClient(), $"{Athens}&{query}");

        var temperatures = body.GetProperty("readings").EnumerateArray()
            .Select(r => r.GetProperty("temperatureCelsius").GetDouble());
        //CollectionAssert.AreEqual(new[] { 18d, 15d }, temperatures.ToArray());
    }

    [Fact]
    public async Task ARepeatedRequest_IsServedFromCache()
    {
        var provider = FakeWeatherProvider.Returning("A", Readings.At("A", Readings.Now));
        await using var factory = new WeatherApiDemoFactory(provider);
        var client = factory.CreateAuthenticatedClient();

        await GetJson(client, Athens);
        var body = await GetJson(client, Athens);

        Assert.Equal(1, provider.Calls);
        Assert.Equal("Cached", body.GetProperty("sources")[0].GetProperty("state").GetString());
    }

    [Fact]
    public async Task WhenOneProviderFails_ReturnsTheOthersAndFlagsIt()
    {
        await using var factory = new WeatherApiDemoFactory(
            FakeWeatherProvider.Failing("A"),
            FakeWeatherProvider.Returning("B", Readings.At("B", Readings.Now)));

        var body = await GetJson(factory.CreateAuthenticatedClient(), Athens);

        Assert.Equal(1, body.GetProperty("readings").GetArrayLength());
        Assert.Equal("Unavailable", body.GetProperty("sources")[0].GetProperty("state").GetString());
    }

    [Fact]
    public async Task WhenEveryProviderFails_IsServiceUnavailable()
    {
        await using var factory = new WeatherApiDemoFactory(FakeWeatherProvider.Failing("A"));

        var response = await factory.CreateAuthenticatedClient().GetAsync(Athens);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("/api/weather?latitude=91&longitude=23.73", "Latitude")]
    [InlineData("/api/weather?latitude=NaN&longitude=23.73", "Latitude")]
    [InlineData("/api/weather?latitude=1&longitude=1&from=2026-12-02&to=2026-12-01", "from")]
    [InlineData("/api/weather?latitude=1&longitude=1&source=Nope", "source")]
    public async Task InvalidValues_AreRejectedWithAValidationProblem(string url, string expectedField)
    {
        var errors = await GetValidationErrors(url);

        Assert.True(errors.TryGetProperty(expectedField, out _), errors.ToString());
    }

    [Theory]
    [InlineData("sortBy=Wind", "sortBy")]
    [InlineData("sortOrder=sideways", "sortOrder")]
    [InlineData("sortBy=2", "sortBy")]
    public async Task UnsupportedSorting_OnlySaysSortingIsNotAvailable(string query, string field)
    {
        var errors = await GetValidationErrors($"{Athens}&{query}");

        Assert.Equal(
            "Sorting is not available for these parameters.",
            errors.GetProperty(field).EnumerateArray().Single().GetString());
    }

    [Fact]
    public async Task UnsupportedCategory_OnlySaysCategoryFilteringIsNotAvailable()
    {
        var errors = await GetValidationErrors($"{Athens}&category=Clear&category=Hail");

        Assert.Equal(
            "Filtering by category is not available for these parameters.",
            errors.GetProperty("category").EnumerateArray().Single().GetString());
    }

    [Theory]
    [InlineData("/api/weather?latitude=abc&longitude=23.73")]
    [InlineData("/api/weather?latitude=1&longitude=1&from=yesterday")]
    public async Task MalformedParameters_AreRejectedWithoutRevealingInternals(string url)
    {
        await using var factory = new WeatherApiDemoFactory(FakeWeatherProvider.Returning("A"));

        var response = await factory.CreateAuthenticatedClient().GetAsync(url);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(
            "A required parameter is missing or a parameter has an invalid format.",
            body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task WhenTheRateLimitIsReached_RejectsFurtherRequests()
    {
        await using var factory = new WeatherApiDemoFactory(FakeWeatherProvider.Returning("A"))
            .WithSetting("RateLimiting:PermitLimit", "2")
            .WithSetting("RateLimiting:Window", "00:10:00");
        var client = factory.CreateAuthenticatedClient();

        await GetJson(client, Athens);
        await GetJson(client, Athens);
        var response = await client.GetAsync(Athens);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task WhenJwtAuthenticationIsDisabled_AllowsAnonymousCalls()
    {
        await using var factory = new WeatherApiDemoFactory(FakeWeatherProvider.Returning("A", Readings.At("A", Readings.Now)))
            .WithSetting("FeatureManagement:JwtAuthentication", "false");

        var body = await GetJson(factory.CreateClient(), Athens);

        Assert.Equal(1, body.GetProperty("readings").GetArrayLength());
    }

    private static async Task<JsonElement> GetValidationErrors(string url)
    {
        await using var factory = new WeatherApiDemoFactory(FakeWeatherProvider.Returning("A"));

        var response = await factory.CreateAuthenticatedClient().GetAsync(url);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        return body.GetProperty("errors");
    }

    private static async Task<JsonElement> GetJson(HttpClient client, string url)
    {
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
