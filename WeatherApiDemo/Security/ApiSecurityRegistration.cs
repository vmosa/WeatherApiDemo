using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace WeatherApiDemo.Server.Security;

public static class ApiSecurityRegistration
{
    public const string AccessPolicy = "ApiAccess";
    public const string RateLimitPolicy = "ApiRateLimit";

    public static IHostApplicationBuilder AddApiSecurity(this IHostApplicationBuilder builder)
    {
        // A flag missing from configuration reads as disabled; refusing to start keeps authentication
        // from being switched off by omission.
        var flagKey = $"FeatureManagement:{FeatureFlags.JwtAuthentication}";
        if (!builder.Configuration.GetSection(flagKey).Exists())
        {
            throw new InvalidOperationException($"'{flagKey}' must be set to true or false.");
        }

        builder.Services.AddFeatureManagement();

        builder.Services.AddAuthentication().AddJwtBearer();
        builder.Services.AddSingleton<IAuthorizationHandler, ApiAccessHandler>();
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(AccessPolicy, policy => policy.AddRequirements(new ApiAccessRequirement()));

        builder.Services.AddOptions<RateLimitingOptions>()
            .Bind(builder.Configuration.GetSection(RateLimitingOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                }

                return ValueTask.CompletedTask;
            };
            options.AddPolicy(RateLimitPolicy, httpContext =>
            {
                var limits = httpContext.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;
                return RateLimitPartition.GetFixedWindowLimiter(
                    PartitionKeyFor(httpContext),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limits.PermitLimit,
                        Window = limits.Window,
                        QueueLimit = 0
                    });
            });
        });

        return builder;
    }

    public static WebApplication UseApiSecurity(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseRateLimiter();

        return app;
    }

    /// <summary>Maps the /api group: every endpoint in it is access-controlled and rate limited.</summary>
    public static RouteGroupBuilder MapApi(this WebApplication app) =>
        app.MapGroup("/api")
            .RequireAuthorization(AccessPolicy)
            .RequireRateLimiting(RateLimitPolicy);

    // Authenticated callers are limited individually; anonymous ones, when the flag allows them, per address.
    private static string PartitionKeyFor(HttpContext httpContext)
    {
        var user = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? httpContext.User.FindFirstValue("sub");
        return user is not null
            ? $"user:{user}"
            : $"address:{httpContext.Connection.RemoteIpAddress}";
    }
}
