using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MovieSearchCase.Domain.Exceptions;
using MovieSearchCase.WebApi.Application.Middleware;
using Xunit;

namespace MovieSearchCase.WebApi.Tests.Application;

public class RequestLoggingMiddlewareTests
{
    private const string Path = "/api/v1/movies/search";

    private readonly IServiceProvider _services = new ServiceCollection()
        .AddLogging()
        .AddMvcCore()
        .Services
        .BuildServiceProvider();

    public static TheoryData<Exception, int, string, string> Failures => new()
    {
        {
            new MovieException("Invalid request", ErrorType.InvalidRequest, "The 'query' parameter is required."),
            400, "Invalid request", "The 'query' parameter is required."
        },
        {
            new MovieException(MovieException.ExceptionTitle, ErrorType.UpstreamServiceUnavailable, "TMDB did not return search results."),
            503, MovieException.ExceptionTitle, "TMDB did not return search results."
        },
        {
            new HttpRequestException("Connection refused (internal-host:443)"),
            503, "Upstream service unavailable", "An upstream service did not respond."
        },
        {
            new InvalidOperationException("Secret connection string leaked"),
            500, "An unexpected error occurred", "The server could not complete the request."
        },
    };

    [Theory]
    [MemberData(nameof(Failures))]
    public async Task InvokeShouldWriteContractProblemDetailsWhenRequestFails(
        Exception exception, int expectedStatus, string expectedTitle, string expectedDetail)
    {
        var httpContext = CreateHttpContext();
        var middleware = new RequestLoggingMiddleware(_ => throw exception, NullLogger<RequestLoggingMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext, _services.GetRequiredService<ProblemDetailsFactory>());

        httpContext.Response.StatusCode.Should().Be(expectedStatus);
        httpContext.Response.ContentType.Should().StartWith("application/problem+json");
        var body = await ReadBodyAsync(httpContext);
        body.GetProperty("type").GetString().Should().Be("about:blank");
        body.GetProperty("title").GetString().Should().Be(expectedTitle);
        body.GetProperty("status").GetInt32().Should().Be(expectedStatus);
        body.GetProperty("detail").GetString().Should().Be(expectedDetail);
        body.GetProperty("instance").GetString().Should().Be(Path);
    }

    [Fact]
    public async Task InvokeShouldWriteNothingWhenClientDisconnects()
    {
        var httpContext = CreateHttpContext();
        using var aborted = new CancellationTokenSource();
        aborted.Cancel();
        httpContext.RequestAborted = aborted.Token;
        var middleware = new RequestLoggingMiddleware(
            _ => throw new OperationCanceledException(),
            NullLogger<RequestLoggingMiddleware>.Instance);

        await middleware.InvokeAsync(httpContext, _services.GetRequiredService<ProblemDetailsFactory>());

        httpContext.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
        httpContext.Response.Body.Length.Should().Be(0);
    }

    private DefaultHttpContext CreateHttpContext()
    {
        var httpContext = new DefaultHttpContext { RequestServices = _services };
        httpContext.Request.Path = Path;
        httpContext.Response.Body = new MemoryStream();
        return httpContext;
    }

    private static async Task<JsonElement> ReadBodyAsync(HttpContext httpContext)
    {
        httpContext.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(httpContext.Response.Body);
        return document.RootElement.Clone();
    }
}
