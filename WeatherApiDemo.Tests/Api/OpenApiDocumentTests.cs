using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using WeatherApiDemo.Tests.Support;

namespace WeatherApiDemo.Tests.Api;

public sealed class OpenApiDocumentTests
{
    [Theory]
    [InlineData("sortBy", "Date,Relevance,Temperature,Category,Source")]
    [InlineData("sortOrder", "Ascending,Descending")]
    public async Task EnumQueryParameters_AreDocumentedAsStringEnums(string parameter, string expectedValues)
    {
        var schema = await GetParameterSchema(parameter);

        Assert.Equal("string", schema.GetProperty("type").GetString());
        Assert.Equal(expectedValues, string.Join(",", schema.GetProperty("enum").EnumerateArray()));
    }

    [Fact]
    public async Task CategoryParameter_IsDocumentedAsAnArrayOfStringEnums()
    {
        var items = (await GetParameterSchema("category")).GetProperty("items");

        Assert.Equal("string", items.GetProperty("type").GetString());
        Assert.Equal(8, items.GetProperty("enum").GetArrayLength());
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    public async Task BearerRequirement_FollowsTheJwtAuthenticationFlag(string flag, bool expectSecurity)
    {
        var operation = await GetWeatherOperation(flag);

        Assert.Equal(expectSecurity, operation.TryGetProperty("security", out _));
    }

    [Theory]
    [InlineData("Production", "/openapi/v1.json", HttpStatusCode.NotFound)]
    [InlineData("Production", "/swagger/index.html", HttpStatusCode.NotFound)]
    [InlineData("Staging", "/openapi/v1.json", HttpStatusCode.OK)]
    [InlineData("Staging", "/swagger/index.html", HttpStatusCode.OK)]
    [InlineData("Development", "/swagger/index.html", HttpStatusCode.OK)]
    public async Task DocumentationIsServedEverywhereButProduction(
        string environment,
        string path,
        HttpStatusCode expected)
    {
        await using var factory = new WeatherApiDemoFactory(FakeWeatherProvider.Returning("A"))
            .WithEnvironment(environment);

        var response = await factory.CreateClient().GetAsync(path);

        Assert.Equal(expected, response.StatusCode);
    }

    private static async Task<JsonElement> GetParameterSchema(string name) =>
        (await GetWeatherOperation()).GetProperty("parameters").EnumerateArray()
            .Single(parameter => parameter.GetProperty("name").GetString() == name)
            .GetProperty("schema");

    private static async Task<JsonElement> GetWeatherOperation(string jwtAuthentication = "true")
    {
        await using var factory = new WeatherApiDemoFactory(FakeWeatherProvider.Returning("A"))
            .WithSetting("FeatureManagement:JwtAuthentication", jwtAuthentication);
        var document = await factory.CreateClient().GetFromJsonAsync<JsonElement>("/openapi/v1.json");

        return document.GetProperty("paths").GetProperty("/api/weather").GetProperty("get").Clone();
    }
}
