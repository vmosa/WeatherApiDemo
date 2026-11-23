using WeatherApiDemo.Server.Weather;
using WeatherApiDemo.Server.Weather.Api;

namespace WeatherApiDemo.Tests.Weather;

public sealed class QueryEnumTests
{
    [Theory]
    [InlineData("Temperature")]
    [InlineData("temperature")]
    [InlineData("TEMPERATURE")]
    [InlineData(" temperature ")]
    public void TryParse_MatchesNamesIgnoringCase(string value)
    {
        Assert.True(QueryEnum<SortField>.TryParse(value, out var parsed));
        Assert.True(parsed.IsDefined);
        Assert.Equal(SortField.Temperature, parsed.Value);
    }

    [Theory]
    [InlineData("Wind")]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("2")]
    [InlineData("42")]
    [InlineData("Rain,Snow")]
    public void TryParse_BindsAnythingButADefinedNameAsUndefined(string? value)
    {
        Assert.True(QueryEnum<WeatherCategory>.TryParse(value, out var parsed));
        Assert.False(parsed.IsDefined);
    }

    [Fact]
    public void ConvertsImplicitlyFromTheEnum()
    {
        QueryEnum<SortOrder> order = SortOrder.Descending;

        Assert.True(order.IsDefined);
        Assert.Equal(SortOrder.Descending, order.Value);
    }
}
