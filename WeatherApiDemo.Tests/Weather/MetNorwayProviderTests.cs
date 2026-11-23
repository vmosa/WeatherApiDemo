using System.Net;
using WeatherApiDemo.Server.Weather;
using WeatherApiDemo.Server.Weather.Providers.MetNorway;
using WeatherApiDemo.Tests.Support;

namespace WeatherApiDemo.Tests.Weather;


public sealed class MetNorwayProviderTests
{
    private const string Response = """
        {
          "properties": {
            "timeseries": [
              {
                "time": "2026-11-20T14:00:00Z",
                "data": {
                  "instant": { "details": { "air_temperature": 21.9, "relative_humidity": 60.7, "wind_speed": 5.5 } },
                  "next_1_hours": { "summary": { "symbol_code": "lightrainshowers_day" }, "details": { "precipitation_amount": 0.4 } },
                  "next_6_hours": { "summary": { "symbol_code": "cloudy" }, "details": { "precipitation_amount": 1.0 } }
                }
              },
              {
                "time": "2026-11-25T00:00:00Z",
                "data": {
                  "instant": { "details": { "air_temperature": 12.0 } },
                  "next_6_hours": { "summary": { "symbol_code": "clearsky_night" }, "details": { "precipitation_amount": 0.0 } }
                }
              },
              {
                "time": "2026-11-29T00:00:00Z",
                "data": { "instant": { "details": { "air_temperature": 10.0 } } }
              }
            ]
          }
        }
        """;

    [Fact]
    public async Task GetForecastAsync_UsesTheShortestAvailablePeriodForConditionAndPrecipitation()
    {
        var provider = CreateProvider(new StubHttpMessageHandler(HttpStatusCode.OK, Response));

        var readings = await provider.GetForecastAsync(new Coordinates(37.98, 23.73), CancellationToken.None);

        Assert.Equal(3, readings.Count);
        Assert.Equal(
            new WeatherReading(
                "MetNorway",
                new DateTimeOffset(2026, 11, 20, 14, 0, 0, TimeSpan.Zero),
                WeatherCategory.Rain,
                "lightrainshowers",
                21.9,
                60.7,
                5.5,
                0.4),
            readings[0]);
        Assert.Equal(WeatherCategory.Clear, readings[1].Category);
        Assert.Equal("clearsky", readings[1].Description);
        Assert.Equal(WeatherCategory.Unknown, readings[2].Category);
        Assert.Null(readings[2].PrecipitationMillimeters);
    }

    [Fact]
    public async Task GetForecastAsync_RequestsTheCompactForecastForTheLocation()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, Response);
        var provider = CreateProvider(handler);

        await provider.GetForecastAsync(new Coordinates(59.91, 10.75), CancellationToken.None);

        Assert.Equal(
            "https://api.example/weatherapi/locationforecast/2.0/compact?lat=59.91&lon=10.75",
            handler.RequestedUris.Single().ToString());
    }


    private static MetNorwayProvider CreateProvider(HttpMessageHandler handler) =>
        new(new StubHttpClientFactory(handler, new Uri("https://api.example/")));
}
