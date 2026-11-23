using Microsoft.AspNetCore.Authorization;
using Microsoft.FeatureManagement;

namespace WeatherApiDemo.Server.Security;

public sealed class ApiAccessRequirement : IAuthorizationRequirement;

/// <summary>
/// Requires an authenticated caller while the JwtAuthentication flag is on. The flag is read per request,
/// so it can be switched without a restart.
/// </summary>
public sealed class ApiAccessHandler(IFeatureManager featureManager) : AuthorizationHandler<ApiAccessRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ApiAccessRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true
            || !await featureManager.IsEnabledAsync(FeatureFlags.JwtAuthentication))
        {
            context.Succeed(requirement);
        }
    }
}
