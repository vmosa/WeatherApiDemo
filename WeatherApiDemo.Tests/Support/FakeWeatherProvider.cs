using WeatherApiDemo.Server.Weather;

namespace WeatherApiDemo.Tests.Support;

internal sealed class FakeWeatherProvider(
    string name,
    Func<Coordinates, CancellationToken, Task<IReadOnlyList<WeatherReading>>> getForecast) : IWeatherProvider
{
    private int calls;

    public string Name => name;

    public int Calls => calls;

    public Task<IReadOnlyList<WeatherReading>> GetForecastAsync(
        Coordinates location,
        CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref calls);
        return getForecast(location, cancellationToken);
    }

    public static FakeWeatherProvider Returning(string name, params WeatherReading[] readings) =>
        new(name, (_, _) => Task.FromResult<IReadOnlyList<WeatherReading>>(readings));

    public static FakeWeatherProvider Failing(string name) =>
        new(name, (_, _) => Task.FromException<IReadOnlyList<WeatherReading>>(new HttpRequestException("boom")));
}

internal static class Readings
{
    public static readonly DateTimeOffset Now = new(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);

    public static WeatherReading At(
        string source,
        DateTimeOffset timestamp,
        WeatherCategory category = WeatherCategory.Clear,
        double temperature = 20) =>
        new(source, timestamp, category, category.ToString(), temperature, 50, 3, 0);
}
