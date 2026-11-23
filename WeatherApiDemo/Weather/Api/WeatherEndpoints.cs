using Microsoft.AspNetCore.Http.HttpResults;
using WeatherApiDemo.Server.Weather.Aggregation;

namespace WeatherApiDemo.Server.Weather.Api;

public static class WeatherEndpoints
{
    public static IEndpointRouteBuilder MapWeatherEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/weather", GetWeatherAsync)
            .WithName("GetAggregatedWeather")
            .WithTags("Weather")
            .WithSummary("Aggregated weather forecast")
            .WithDescription(
                "Forecast readings from every provider for a location, filterable by time, category and source, and sortable.")
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        return app;
    }

    internal static async Task<Results<Ok<AggregatedWeather>, ValidationProblem, ProblemHttpResult>> GetWeatherAsync(
        [AsParameters] WeatherQuery query,
        WeatherAggregator aggregator,
        CancellationToken cancellationToken)
    {
        var unknownSources = query.Source?
            .Except(aggregator.ProviderNames, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (unknownSources is { Length: > 0 })
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["source"] =
                [
                    $"Unknown source(s): {string.Join(", ", unknownSources)}. " +
                    $"Available: {string.Join(", ", aggregator.ProviderNames)}."
                ]
            });
        }

        var weather = await aggregator.GetAsync(query, cancellationToken);

        if (weather.Sources.All(source => source.State == SourceState.Unavailable))
        {
            return TypedResults.Problem(
                title: "No weather source is available.",
                detail: "Every requested provider failed and none had a cached forecast for this location.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return TypedResults.Ok(weather);
    }
}
