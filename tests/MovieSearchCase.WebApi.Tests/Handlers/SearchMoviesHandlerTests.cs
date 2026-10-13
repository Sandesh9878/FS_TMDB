using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MovieSearchCase.Domain.Entities;
using MovieSearchCase.Domain.Exceptions;
using MovieSearchCase.Domain.Interfaces.Services;
using MovieSearchCase.WebApi.Handlers.Movies;
using Xunit;
using ApiMovie = MovieSearchCase.WebApi.Models.Movies.Movie;
using ApiPagedResult = MovieSearchCase.WebApi.Models.PagedResult<MovieSearchCase.WebApi.Models.Movies.Movie>;

namespace MovieSearchCase.WebApi.Tests.Handlers;

public class SearchMoviesHandlerTests
{
    private readonly Mock<IMovieService> _movieServiceMock = new();

    public SearchMoviesHandlerTests()
    {
        _movieServiceMock
            .Setup(service => service.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedResult<Movie>
            {
                Page = 2,
                TotalPages = 9,
                TotalResults = 175,
                Results = [new() { Id = 268, Title = "Batman", VoteAverage = 7.2 }],
            });
    }

    [Fact]
    public async Task HandleShouldReturnOkWithMappedPageWhenInputIsValid()
    {
        var result = await HandleAsync(" batman ", "2");

        _movieServiceMock.Verify(service => service.SearchAsync("batman", 2, It.IsAny<CancellationToken>()), Times.Once);
        var page = result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<ApiPagedResult>().Subject;
        page.Page.Should().Be(2);
        page.TotalPages.Should().Be(9);
        page.TotalResults.Should().Be(175);
        page.Results.Should().ContainSingle().Which.Should().BeEquivalentTo(new ApiMovie { Id = 268, Title = "Batman", VoteAverage = 7.2 });
    }

    [Fact]
    public async Task HandleShouldDefaultToPageOneWhenPageIsMissing()
    {
        await HandleAsync("batman", null);

        _movieServiceMock.Verify(service => service.SearchAsync("batman", 1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("500", 500)]
    public async Task HandleShouldAcceptPageBoundaries(string page, int expectedPage)
    {
        await HandleAsync("batman", page);

        _movieServiceMock.Verify(service => service.SearchAsync("batman", expectedPage, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HandleShouldThrowInvalidRequestWhenQueryIsBlank(string? query)
    {
        var act = () => HandleAsync(query, "1");

        await act.Should().ThrowAsync<MovieException>()
            .Where(exception => exception.ErrorCode == ErrorType.InvalidRequest
                && exception.Message == "The 'query' parameter is required.");
        _movieServiceMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("0")]
    [InlineData("501")]
    [InlineData("-1")]
    [InlineData("abc")]
    [InlineData("1.5")]
    [InlineData("+5")]
    [InlineData(" 5")]
    [InlineData("1e2")]
    [InlineData("99999999999")]
    public async Task HandleShouldThrowInvalidRequestWhenPageIsInvalid(string page)
    {
        var act = () => HandleAsync("batman", page);

        await act.Should().ThrowAsync<MovieException>()
            .Where(exception => exception.ErrorCode == ErrorType.InvalidRequest
                && exception.Message == "The 'page' parameter must be a whole number from 1 to 500.");
        _movieServiceMock.VerifyNoOtherCalls();
    }

    private Task<IActionResult> HandleAsync(string? query, string? page) =>
        new SearchMoviesHandler(_movieServiceMock.Object, query, page).HandleAsync(new DefaultHttpContext().Request);
}
