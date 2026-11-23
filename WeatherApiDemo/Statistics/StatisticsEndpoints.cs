using Microsoft.AspNetCore.Http.HttpResults;
using WeatherApiDemo.Server.Weather;

namespace WeatherApiDemo.Server.Statistics;

public static class StatisticsEndpoints
{
    public static IEndpointRouteBuilder MapStatisticsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/statistics", GetStatistics)
            .WithName("GetRequestStatistics")
            .WithTags("Statistics")
            .WithSummary("External API request statistics")
            .WithDescription(
                "Per provider: total and failed requests, average response time, recent window and performance buckets.")
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        return app;
    }

    internal static Ok<IReadOnlyList<ProviderStatistics>> GetStatistics(
        IEnumerable<IWeatherProvider> providers,
        RequestStatisticsStore statistics)
    {
        IReadOnlyList<ProviderStatistics> result = providers
            .Select(provider => statistics.GetStatistics(provider.Name))
            .ToList();

        return TypedResults.Ok(result);
    }
}
