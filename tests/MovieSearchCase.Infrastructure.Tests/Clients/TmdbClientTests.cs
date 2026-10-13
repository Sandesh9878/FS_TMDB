using System.Net;
using System.Text;
using FluentAssertions;
using MovieSearchCase.Domain.Exceptions;
using MovieSearchCase.Infrastructure.Clients;
using Xunit;

namespace MovieSearchCase.Infrastructure.Tests.Clients;

public class TmdbClientTests
{
    private const string BatmanPage = """
        {
          "page": 2,
          "total_pages": 9,
          "total_results": 175,
          "results": [
            {
              "id": 268,
              "title": "Batman",
              "overview": "Batman must face his most ruthless nemesis.",
              "poster_path": "/poster.jpg",
              "backdrop_path": null,
              "vote_average": 7.2,
              "release_date": "1989-06-21"
            }
          ]
        }
        """;

    [Fact]
    public async Task SearchMoviesAsyncShouldEscapeQueryAndMapResponse()
    {
        HttpRequestMessage? sentRequest = null;
        var client = CreateClient((request, _) =>
        {
            sentRequest = request;
            return Task.FromResult(Json(BatmanPage));
        });

        var result = await client.SearchMoviesAsync("Fast & Furious #7", 2, CancellationToken.None);

        sentRequest!.RequestUri!.PathAndQuery.Should().Be("/3/search/movie?query=Fast%20%26%20Furious%20%237&page=2");
        result.Page.Should().Be(2);
        result.TotalPages.Should().Be(9);
        result.TotalResults.Should().Be(175);
        var movie = result.Results.Should().ContainSingle().Subject;
        movie.Id.Should().Be(268);
        movie.Title.Should().Be("Batman");
        movie.BackdropPath.Should().BeNull();
        movie.ReleaseDate.Should().Be(new DateOnly(1989, 6, 21));
    }

    [Fact]
    public async Task SearchMoviesAsyncShouldReturnZeroTotalPagesWhenNothingMatches()
    {
        var client = CreateClient((_, _) =>
            Task.FromResult(Json("""{ "page": 1, "total_pages": 1, "total_results": 0, "results": [] }""")));

        var result = await client.SearchMoviesAsync("zzqqxx", 1, CancellationToken.None);

        result.TotalPages.Should().Be(0);
        result.TotalResults.Should().Be(0);
        result.Results.Should().BeEmpty();
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task SearchMoviesAsyncShouldThrowUpstreamUnavailableWhenTmdbFails(HttpStatusCode statusCode)
    {
        var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(statusCode)));

        var act = () => client.SearchMoviesAsync("batman", 1, CancellationToken.None);

        await act.Should().ThrowAsync<MovieException>()
            .Where(exception => exception.ErrorCode == ErrorType.UpstreamServiceUnavailable);
    }

    [Fact]
    public async Task SearchMoviesAsyncShouldThrowUpstreamUnavailableWhenTmdbIsUnreachable()
    {
        var client = CreateClient((_, _) => throw new HttpRequestException("Connection refused"));

        var act = () => client.SearchMoviesAsync("batman", 1, CancellationToken.None);

        await act.Should().ThrowAsync<MovieException>()
            .Where(exception => exception.ErrorCode == ErrorType.UpstreamServiceUnavailable);
    }

    [Fact]
    public async Task SearchMoviesAsyncShouldThrowUpstreamUnavailableWhenTmdbTimesOut()
    {
        var client = CreateClient(
            async (_, cancellationToken) =>
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
                return Json(BatmanPage);
            },
            TimeSpan.FromMilliseconds(50));

        var act = () => client.SearchMoviesAsync("batman", 1, CancellationToken.None);

        await act.Should().ThrowAsync<MovieException>()
            .Where(exception => exception.ErrorCode == ErrorType.UpstreamServiceUnavailable);
    }

    [Fact]
    public async Task SearchMoviesAsyncShouldLetCancellationPropagateWhenCallerCancels()
    {
        var client = CreateClient(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            return Json(BatmanPage);
        });
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var act = () => client.SearchMoviesAsync("batman", 1, cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task SearchMoviesAsyncShouldThrowInvalidOperationWhenTokenIsRejected()
    {
        var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));

        var act = () => client.SearchMoviesAsync("batman", 1, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SearchMoviesAsyncShouldThrowUpstreamUnavailableWhenBodyIsNull()
    {
        var client = CreateClient((_, _) => Task.FromResult(Json("null")));

        var act = () => client.SearchMoviesAsync("batman", 1, CancellationToken.None);

        await act.Should().ThrowAsync<MovieException>()
            .Where(exception => exception.ErrorCode == ErrorType.UpstreamServiceUnavailable);
    }

    [Fact]
    public async Task SearchMoviesAsyncShouldThrowUpstreamUnavailableWhenBodyIsNotJson()
    {
        var client = CreateClient((_, _) => Task.FromResult(Json("<html>not json</html>")));

        var act = () => client.SearchMoviesAsync("batman", 1, CancellationToken.None);

        await act.Should().ThrowAsync<MovieException>()
            .Where(exception => exception.ErrorCode == ErrorType.UpstreamServiceUnavailable);
    }

    [Fact]
    public async Task GetTrendingMoviesAsyncShouldCallTrendingEndpointAndMapResults()
    {
        HttpRequestMessage? sentRequest = null;
        var client = CreateClient((request, _) =>
        {
            sentRequest = request;
            return Task.FromResult(Json(BatmanPage));
        });

        var result = await client.GetTrendingMoviesAsync(CancellationToken.None);

        sentRequest!.RequestUri!.PathAndQuery.Should().Be("/3/trending/movie/week");
        result.Should().ContainSingle().Which.Title.Should().Be("Batman");
    }

    [Fact]
    public async Task GetMovieDetailsAsyncShouldMapDetailsAndNormalizeEmptyValues()
    {
        HttpRequestMessage? sentRequest = null;
        var client = CreateClient((request, _) =>
        {
            sentRequest = request;
            return Task.FromResult(Json("""
                {
                  "id": 1000000,
                  "title": "The Ladies Room",
                  "vote_average": 0,
                  "release_date": "2006-10-24",
                  "runtime": 0,
                  "tagline": "",
                  "genres": [{ "id": 99, "name": "Documentary" }, { "id": 18, "name": "Drama" }]
                }
                """));
        });

        var result = await client.GetMovieDetailsAsync(1000000, CancellationToken.None);

        sentRequest!.RequestUri!.PathAndQuery.Should().Be("/3/movie/1000000");
        result.Id.Should().Be(1000000);
        result.Title.Should().Be("The Ladies Room");
        result.ReleaseDate.Should().Be(new DateOnly(2006, 10, 24));
        result.Runtime.Should().BeNull();
        result.Tagline.Should().BeNull();
        result.Genres.Should().Equal("Documentary", "Drama");
    }

    [Fact]
    public async Task GetMovieDetailsAsyncShouldKeepRuntimeAndTaglineWhenPresent()
    {
        var client = CreateClient((_, _) => Task.FromResult(Json("""
            { "id": 268, "title": "Batman", "runtime": 126, "tagline": "Have you ever danced with the devil?", "genres": [] }
            """)));

        var result = await client.GetMovieDetailsAsync(268, CancellationToken.None);

        result.Runtime.Should().Be(126);
        result.Tagline.Should().Be("Have you ever danced with the devil?");
        result.Genres.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMovieVideosAsyncShouldMapVideos()
    {
        HttpRequestMessage? sentRequest = null;
        var client = CreateClient((request, _) =>
        {
            sentRequest = request;
            return Task.FromResult(Json("""
                {
                  "id": 268,
                  "results": [
                    { "key": "dgC9Q0uhX70", "name": "Official Trailer", "site": "YouTube", "type": "Trailer", "size": 1080 }
                  ]
                }
                """));
        });

        var result = await client.GetMovieVideosAsync(268, CancellationToken.None);

        sentRequest!.RequestUri!.PathAndQuery.Should().Be("/3/movie/268/videos");
        var video = result.Should().ContainSingle().Subject;
        video.Key.Should().Be("dgC9Q0uhX70");
        video.Name.Should().Be("Official Trailer");
        video.Site.Should().Be("YouTube");
        video.Type.Should().Be("Trailer");
    }

    [Fact]
    public async Task GetMovieVideosAsyncShouldReturnEmptyListWhenMovieHasNoVideos()
    {
        var client = CreateClient((_, _) => Task.FromResult(Json("""{ "id": 268, "results": [] }""")));

        var result = await client.GetMovieVideosAsync(268, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMovieDetailsAsyncShouldThrowNotFoundWhenTmdbReturns404()
    {
        var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        var act = () => client.GetMovieDetailsAsync(999999999, CancellationToken.None);

        await act.Should().ThrowAsync<MovieException>()
            .Where(exception => exception.ErrorCode == ErrorType.EntityNotFound
                && exception.Message == "Movie 999999999 does not exist.");
    }

    [Fact]
    public async Task GetMovieVideosAsyncShouldThrowNotFoundWhenTmdbReturns404()
    {
        var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        var act = () => client.GetMovieVideosAsync(999999999, CancellationToken.None);

        await act.Should().ThrowAsync<MovieException>()
            .Where(exception => exception.ErrorCode == ErrorType.EntityNotFound);
    }

    [Fact]
    public async Task GetTrendingMoviesAsyncShouldThrowUpstreamUnavailableWhenTmdbReturns404()
    {
        var client = CreateClient((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));

        var act = () => client.GetTrendingMoviesAsync(CancellationToken.None);

        await act.Should().ThrowAsync<MovieException>()
            .Where(exception => exception.ErrorCode == ErrorType.UpstreamServiceUnavailable);
    }

    private static TmdbClient CreateClient(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send,
        TimeSpan? timeout = null) =>
        new(new HttpClient(new FakeHttpMessageHandler(send))
        {
            BaseAddress = new Uri("https://api.themoviedb.org/3/"),
            Timeout = timeout ?? TimeSpan.FromSeconds(10),
        });

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed class FakeHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            send(request, cancellationToken);
    }
}
