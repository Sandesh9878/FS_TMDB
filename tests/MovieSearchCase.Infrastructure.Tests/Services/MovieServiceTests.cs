using FluentAssertions;
using Moq;
using MovieSearchCase.Domain.Entities;
using MovieSearchCase.Domain.Interfaces.Clients;
using MovieSearchCase.Infrastructure.Services;
using Xunit;

namespace MovieSearchCase.Infrastructure.Tests.Services;

public class MovieServiceTests
{
    [Fact]
    public async Task GetTrendingAsyncShouldReturnMoviesFromTmdbClient()
    {
        var expectedMovies = new List<Movie>
        {
            new()
            {
                Id = 42,
                Title = "The Answer",
                VoteAverage = 9.2,
            },
        };

        var tmdbClientMock = new Mock<ITmdbClient>();
        tmdbClientMock
            .Setup(client => client.GetTrendingMoviesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMovies);

        var movieService = new MovieService(tmdbClientMock.Object);

        var result = await movieService.GetTrendingAsync(CancellationToken.None);

        result.Should().BeEquivalentTo(expectedMovies);
    }

    [Fact]
    public async Task SearchAsyncShouldReturnPageFromTmdbClient()
    {
        var expectedPage = new PagedResult<Movie>
        {
            Page = 2,
            TotalPages = 9,
            TotalResults = 175,
            Results = [new() { Id = 268, Title = "Batman", VoteAverage = 7.2 }],
        };

        var tmdbClientMock = new Mock<ITmdbClient>();
        tmdbClientMock
            .Setup(client => client.SearchMoviesAsync("batman", 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPage);

        var movieService = new MovieService(tmdbClientMock.Object);

        var result = await movieService.SearchAsync("batman", 2, CancellationToken.None);

        result.Should().BeSameAs(expectedPage);
    }
}
