using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using WeatherApiDemo.Server.Security;

namespace WeatherApiDemo.Tests.Security;

public sealed class ApiSecurityRegistrationTests
{
    [Fact]
    public void WhenTheJwtAuthenticationFlagIsNotConfigured_RefusesToStart()
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());

        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddApiSecurity());

        StringAssert.Contains(exception.Message, "FeatureManagement:JwtAuthentication");
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    public void WhenTheJwtAuthenticationFlagIsConfigured_Registers(string value)
    {
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?> { ["FeatureManagement:JwtAuthentication"] = value });

        builder.AddApiSecurity();
    }
}
