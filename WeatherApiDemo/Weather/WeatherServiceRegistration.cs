using Microsoft.Extensions.Options;
using StackExchange.Redis;
using WeatherApiDemo.Server.Monitoring;
using WeatherApiDemo.Server.Statistics;
using WeatherApiDemo.Server.Weather.Aggregation;
using WeatherApiDemo.Server.Weather.Caching;
using WeatherApiDemo.Server.Weather.Providers.MetNorway;
using WeatherApiDemo.Server.Weather.Providers.OpenMeteo;
using WeatherApiDemo.Server.Weather.Providers.OpenWeatherMap;

namespace WeatherApiDemo.Server.Weather;

public static class WeatherServiceRegistration
{
    public static IHostApplicationBuilder AddWeatherAggregation(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<ForecastCache>();
        builder.Services.AddSingleton<ProviderForecastFetcher>();
        builder.Services.AddSingleton<WeatherAggregator>();

        builder.AddWeatherProvider<OpenWeatherMapProvider>(OpenWeatherMapProvider.ProviderName, requiresApiKey: true);
        builder.AddWeatherProvider<OpenMeteoProvider>(OpenMeteoProvider.ProviderName);
        builder.AddWeatherProvider<MetNorwayProvider>(MetNorwayProvider.ProviderName);

        builder.AddRedisForecastCache();
        builder.AddRequestStatistics();

        return builder;
    }

    /// <summary>Registers an external API with its own options, HTTP client and resilience pipeline.</summary>
    public static IHostApplicationBuilder AddWeatherProvider<TProvider>(
        this IHostApplicationBuilder builder,
        string name,
        bool requiresApiKey = false)
        where TProvider : class, IWeatherProvider
    {
        var section = builder.Configuration.GetSection($"{WeatherProviderOptions.SectionName}:{name}");
        if (!section.GetValue(nameof(WeatherProviderOptions.Enabled), defaultValue: true))
        {
            return builder;
        }

        builder.Services.AddOptions<WeatherProviderOptions>(name)
            .Bind(section)
            .ValidateDataAnnotations()
            .Validate(
                options => !requiresApiKey || !string.IsNullOrWhiteSpace(options.ApiKey),
                $"{name} requires an ApiKey; set one or disable the provider.")
            .Validate(
                options => options.FallbackDuration >= options.CacheDuration,
                $"{name} FallbackDuration must not be shorter than CacheDuration.")
            .ValidateOnStart();

        builder.Services.AddSingleton<IWeatherProvider, TProvider>();

        builder.Services
            .AddHttpClient(name, (services, client) =>
            {
                var options = services.GetRequiredService<IOptionsMonitor<WeatherProviderOptions>>().Get(name);
                client.BaseAddress = options.BaseAddress;
                client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
            })
            .AddStandardResilienceHandler(section.GetSection("Resilience"));

        return builder;
    }

    private static void AddRedisForecastCache(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("cache")
            ?? throw new InvalidOperationException("Connection string 'cache' is required.");

        builder.Services.AddStackExchangeRedisCache(options =>
        {
            var configuration = ConfigurationOptions.Parse(connectionString);

            // While Redis is down, fail cache calls immediately rather than queueing them,
            // so requests fall through to the providers without waiting for timeouts.
            configuration.AbortOnConnectFail = false;
            configuration.BacklogPolicy = BacklogPolicy.FailFast;

            options.ConfigurationOptions = configuration;
            options.InstanceName = "weatherapi:";
        });
    }

    private static void AddRequestStatistics(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions<StatisticsOptions>()
            .Bind(builder.Configuration.GetSection(StatisticsOptions.SectionName))
            .Validate(options => options.FastThreshold <= options.SlowThreshold, "FastThreshold must not exceed SlowThreshold.")
            .ValidateOnStart();
        builder.Services.AddSingleton<RequestStatisticsStore>();

        var monitoringSection = builder.Configuration.GetSection(PerformanceMonitoringOptions.SectionName);
        builder.Services.AddOptions<PerformanceMonitoringOptions>()
            .Bind(monitoringSection)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        builder.Services.AddSingleton<PerformanceAnomalyDetector>();

        if (monitoringSection.GetValue(nameof(PerformanceMonitoringOptions.Enabled), defaultValue: true))
        {
            builder.Services.AddHostedService<PerformanceMonitoringService>();
        }
    }
}
