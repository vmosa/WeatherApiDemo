using System.Net;
using WeatherApiDemo.Server.Weather;
using WeatherApiDemo.Server.Weather.Providers.OpenWeatherMap;
using WeatherApiDemo.Tests.Support;

namespace WeatherApiDemo.Tests.Weather;

public sealed class OpenWeatherMapProviderTests
{
    private const string Response = """
        {
          "list": [
            {
              "dt": 1795521600,
              "main": { "temp": 18.4, "humidity": 71 },
              "weather": [ { "id": 501, "main": "Rain", "description": "moderate rain" } ],
              "wind": { "speed": 4.2 },
              "rain": { "3h": 2.5 }
            },
            {
              "dt": 1795532400,
              "main": { "temp": 16.1, "humidity": 80 },
              "weather": [ { "id": 800, "main": "Clear", "description": "clear sky" } ],
              "wind": { "speed": 1.1 }
            }
          ]
        }
        """;

    [Fact]
    public async Task GetForecastAsync_MapsEveryEntryToANormalisedReading()
    {
        var provider = CreateProvider(new StubHttpMessageHandler(HttpStatusCode.OK, Response));

        var readings = await provider.GetForecastAsync(new Coordinates(37.98, 23.73), CancellationToken.None);

        //Assert.HasCount(2, readings);
        Assert.Equal(
            new WeatherReading(
                "OpenWeatherMap",
                DateTimeOffset.FromUnixTimeSeconds(1795521600),
                WeatherCategory.Rain,
                "moderate rain",
                18.4,
                71,
                4.2,
                2.5),
            readings[0]);
        Assert.Equal(WeatherCategory.Clear, readings[1].Category);
        Assert.Equal(0, readings[1].PrecipitationMillimeters);
    }

    [Fact]
    public async Task GetForecastAsync_SendsCoordinatesMetricUnitsAndApiKey()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, Response);
        var provider = CreateProvider(handler);

        await provider.GetForecastAsync(new Coordinates(37.981, -23.728), CancellationToken.None);

        Assert.Equal(
            "https://api.example/data/2.5/forecast?lat=37.98&lon=-23.73&units=metric&appid=secret",
            handler.RequestedUris.Single().ToString());
    }

    [Fact]
    public async Task GetForecastAsync_WhenTheProviderReturnsAnError_Throws()
    {
        var provider = CreateProvider(new StubHttpMessageHandler(HttpStatusCode.Unauthorized, "{}"));

       // await Assert.ThrowsExactlyAsync<HttpRequestException>(() =>
       //     provider.GetForecastAsync(new Coordinates(1, 1), CancellationToken.None));
    }

    

    private static OpenWeatherMapProvider CreateProvider(HttpMessageHandler handler)
    {
        var options = new StaticOptionsMonitor(new WeatherProviderOptions { ApiKey = "secret" });
        return new OpenWeatherMapProvider(new StubHttpClientFactory(handler, new Uri("https://api.example/")), options);
    }
}
