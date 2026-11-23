using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using WeatherApiDemo.Server.Security;
using WeatherApiDemo.Tests.Support;

namespace WeatherApiDemo.Tests.Security;

public sealed class ApiAccessHandlerTests
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    private static readonly ClaimsPrincipal Authenticated =
        new(new ClaimsIdentity([new Claim("sub", "tester")], authenticationType: "Bearer"));

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    public async Task RequiresAnAuthenticatedUserOnlyWhileJwtAuthenticationIsEnabled(
        bool jwtEnabled,
        bool authenticated,
        bool expectedAccess)
    {
        var requirement = new ApiAccessRequirement();
        var context = new AuthorizationHandlerContext(
            [requirement],
            authenticated ? Authenticated : Anonymous,
            resource: null);
        var handler = new ApiAccessHandler(new FakeFeatureManager(FeatureFlags.JwtAuthentication, jwtEnabled));

        await handler.HandleAsync(context);

        Assert.Equal(expectedAccess, context.HasSucceeded);
    }
}
