using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using WeatherApiDemo.Server.Weather;

namespace WeatherApiDemo.Tests.Api;

/// <summary>Hosts the real application with fake providers, an in-memory cache and a throwaway signing key.</summary>
internal sealed class WeatherApiDemoFactory(params IWeatherProvider[] providers) : WebApplicationFactory<Program>
{
    private const string Issuer = "weatherapi-tests";
    private const string Audience = "weatherapi-api";

    private static readonly SymmetricSecurityKey SigningKey = new(RandomNumberGenerator.GetBytes(32));

    private readonly Dictionary<string, string> settings = new()
    {
        ["ConnectionStrings:cache"] = "localhost:1",
        ["WeatherProviders:OpenWeatherMap:ApiKey"] = "test-key",
        ["PerformanceMonitoring:Enabled"] = "false",
        ["FeatureManagement:JwtAuthentication"] = "true"
    };

    private string environment = "Development";

    /// <summary>Overrides a configuration value; call before the first client is created.</summary>
    public WeatherApiDemoFactory WithSetting(string key, string value)
    {
        settings[key] = value;
        return this;
    }

    /// <summary>Hosts the application in the given environment instead of Development.</summary>
    public WeatherApiDemoFactory WithEnvironment(string environment)
    {
        this.environment = environment;
        return this;
    }

    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", CreateToken());
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(environment);
        foreach (var (key, value) in settings)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IWeatherProvider>();
            foreach (var provider in providers)
            {
                services.AddSingleton<IWeatherProvider>(provider);
            }

            services.RemoveAll<IDistributedCache>();
            services.AddDistributedMemoryCache();

            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = Issuer,
                    ValidAudience = Audience,
                    IssuerSigningKey = SigningKey
                });
        });
    }

    private static string CreateToken() =>
        new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            Subject = new ClaimsIdentity([new Claim("sub", "tester")]),
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256)
        });
}
