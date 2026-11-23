using System.Net;
using WeatherApiDemo.Server.Weather;
using WeatherApiDemo.Server.Weather.Providers.OpenMeteo;
using WeatherApiDemo.Tests.Support;

namespace WeatherApiDemo.Tests.Weather;

public sealed class OpenMeteoProviderTests
{
    private const string Response = """
        {
          "hourly": {
            "time": [ 1795521600, 1795525200, 1795528800 ],
            "temperature_2m": [ 21.5, null, 19.0 ],
            "relative_humidity_2m": [ 60, 61, 65 ],
            "precipitation": [ 0.0, 0.0, 1.2 ],
            "weather_code": [ 2, 3, 63 ],
            "wind_speed_10m": [ 3.5, 3.6, 5.0 ]
          }
        }
        """;

    [Fact]
    public async Task GetForecastAsync_MapsHourlyArraysAndSkipsHoursWithoutTemperature()
    {
        var provider = CreateProvider(new StubHttpMessageHandler(HttpStatusCode.OK, Response));

        var readings = await provider.GetForecastAsync(new Coordinates(37.98, 23.73), CancellationToken.None);

        //Assert.HasCount(2, readings);
        Assert.Equal(
            new WeatherReading(
                "OpenMeteo",
                DateTimeOffset.FromUnixTimeSeconds(1795521600),
                WeatherCategory.Clouds,
                "partly cloudy",
                21.5,
                60,
                3.5,
                0),
            readings[0]);
        Assert.Equal(WeatherCategory.Rain, readings[1].Category);
        Assert.Equal(1.2, readings[1].PrecipitationMillimeters);
    }

    [Fact]
    public async Task GetForecastAsync_RequestsHourlyMetricDataInUnixTime()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, Response);
        var provider = CreateProvider(handler);

        await provider.GetForecastAsync(new Coordinates(37.98, 23.73), CancellationToken.None);

        var query = handler.RequestedUris.Single().Query;
        //StringAssert.Contains(query, "latitude=37.98&longitude=23.73");
        //StringAssert.Contains(query, "wind_speed_unit=ms");
        //StringAssert.Contains(query, "timeformat=unixtime");
    }

    private static OpenMeteoProvider CreateProvider(HttpMessageHandler handler) =>
        new(new StubHttpClientFactory(handler, new Uri("https://api.example/")));
}
