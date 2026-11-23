using Microsoft.AspNetCore.Diagnostics;

namespace WeatherApiDemo.Server;

/// <summary>
/// Answers request binding failures with a 400 and a fixed message; the framework's own message names
/// internal types, so it is only logged.
/// </summary>
internal sealed class BadRequestExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<BadRequestExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not BadHttpRequestException badRequest)
        {
            return false;
        }

        logger.LogInformation(exception, "Request binding failed");

        httpContext.Response.StatusCode = badRequest.StatusCode;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Status = badRequest.StatusCode,
                Title = "The request is invalid.",
                Detail = "A required parameter is missing or a parameter has an invalid format."
            }
        });
    }
}
