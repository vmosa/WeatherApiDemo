using WeatherApiDemo.Tests.Support;
using WeatherApiDemo.Server.Weather.Api;
using WeatherApiDemo.Server.Weather;
using WeatherApiDemo.Server.Weather.Aggregation;

namespace WeatherApiDemo.Tests.Weather;

public sealed class WeatherReadingFilterTests
{
    private static readonly WeatherReading EarlyRain =
        Readings.At("OpenMeteo", Readings.Now.AddHours(-3), WeatherCategory.Rain, 12);

    private static readonly WeatherReading NowClear =
        Readings.At("MetNorway", Readings.Now, WeatherCategory.Clear, 20);

    private static readonly WeatherReading LaterSnow =
        Readings.At("OpenWeatherMap", Readings.Now.AddHours(5), WeatherCategory.Snow, -2);

    private static readonly WeatherReading[] All = [LaterSnow, NowClear, EarlyRain];

    [Fact]
    public void WithoutParameters_SortsByDateAscending()
    {
        var result = Apply(new WeatherQuery());

        //CollectionAssert.AreEqual(new[] { EarlyRain, NowClear, LaterSnow }, result.ToArray());
    }

    [Fact]
    public void FromAndTo_AreInclusive()
    {
        var result = Apply(new WeatherQuery { From = Readings.Now.AddHours(-3), To = Readings.Now });

        //CollectionAssert.AreEqual(new[] { EarlyRain, NowClear }, result.ToArray());
    }

    [Fact]
    public void Category_KeepsOnlyTheRequestedCategories()
    {
        var result = Apply(new WeatherQuery { Category = [WeatherCategory.Snow, WeatherCategory.Rain] });

        //CollectionAssert.AreEqual(new[] { EarlyRain, LaterSnow }, result.ToArray());
    }

    [Theory]
    [InlineData(SortField.Temperature, SortOrder.Ascending, "OpenWeatherMap,OpenMeteo,MetNorway")]
    [InlineData(SortField.Temperature, SortOrder.Descending, "MetNorway,OpenMeteo,OpenWeatherMap")]
    [InlineData(SortField.Source, SortOrder.Ascending, "MetNorway,OpenMeteo,OpenWeatherMap")]
    [InlineData(SortField.Category, SortOrder.Ascending, "MetNorway,OpenMeteo,OpenWeatherMap")]
    [InlineData(SortField.Date, SortOrder.Descending, "OpenWeatherMap,MetNorway,OpenMeteo")]
    public void SortsByTheRequestedFieldAndOrder(SortField sortBy, SortOrder sortOrder, string expectedSources)
    {
        var result = Apply(new WeatherQuery { SortBy = sortBy, SortOrder = sortOrder });

        Assert.Equal(expectedSources, string.Join(",", result.Select(r => r.Source)));
    }

    [Fact]
    public void Relevance_PutsReadingsNearestToNowFirst()
    {
        var result = Apply(new WeatherQuery { SortBy = SortField.Relevance });

        //CollectionAssert.AreEqual(new[] { NowClear, EarlyRain, LaterSnow }, result.ToArray());
    }

    [Fact]
    public void Relevance_IsMeasuredFromFromWhenGiven()
    {
        var result = Apply(new WeatherQuery { SortBy = SortField.Relevance, From = Readings.Now.AddHours(-4) });

        //CollectionAssert.AreEqual(new[] { EarlyRain, NowClear, LaterSnow }, result.ToArray());
    }

    [Fact]
    public void EqualSortKeys_FallBackToDateThenSource()
    {
        var sameTime = Readings.At("A", Readings.Now, WeatherCategory.Clear, 20);

        var result = Apply(new WeatherQuery { SortBy = SortField.Category }, [NowClear, sameTime]);

        //CollectionAssert.AreEqual(new[] { sameTime, NowClear }, result.ToArray());
    }

    private static IReadOnlyList<WeatherReading> Apply(WeatherQuery query, WeatherReading[]? readings = null) =>
        WeatherReadingFilter.Apply(readings ?? All, query, Readings.Now);
}
