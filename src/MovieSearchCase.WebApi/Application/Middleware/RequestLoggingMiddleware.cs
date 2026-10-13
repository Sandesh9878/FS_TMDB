using Microsoft.AspNetCore.Mvc.Infrastructure;
using MovieSearchCase.Domain.Exceptions;

namespace MovieSearchCase.WebApi.Application.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext, ProblemDetailsFactory problemDetailsFactory)
    {
        try
        {
            await _next(httpContext);
        }
        catch (OperationCanceledException) when (httpContext.RequestAborted.IsCancellationRequested)
        {
            _logger.LogInformation("Request to {Path} was cancelled by the client", httpContext.Request.Path);
        }
        catch (Exception exception)
        {
            var (statusCode, title, detail) = exception switch
            {
                ProblemDetailsException problemDetailsException => (
                    problemDetailsException.ErrorCode.ToStatusCode(),
                    problemDetailsException.Title,
                    problemDetailsException.Message),
                HttpRequestException => (
                    StatusCodes.Status503ServiceUnavailable,
                    "Upstream service unavailable",
                    "An upstream service did not respond."),
                _ => (
                    StatusCodes.Status500InternalServerError,
                    "An unexpected error occurred",
                    "The server could not complete the request."),
            };

            if (statusCode < StatusCodes.Status500InternalServerError)
            {
                _logger.LogWarning("Request to {Path} failed with status {StatusCode}: {Detail}", httpContext.Request.Path, statusCode, detail);
            }
            else
            {
                _logger.LogError(exception, "Request to {Path} failed with status {StatusCode}", httpContext.Request.Path, statusCode);
            }

            var problemDetails = problemDetailsFactory.CreateProblemDetails(
                httpContext,
                statusCode,
                title,
                type: "about:blank",
                detail: detail,
                instance: httpContext.Request.Path);

            httpContext.Response.StatusCode = statusCode;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, options: null, contentType: "application/problem+json");
        }
    }
}

public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app) =>
        app.UseMiddleware<RequestLoggingMiddleware>();
}
