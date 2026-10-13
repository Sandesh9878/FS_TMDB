using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MovieSearchCase.Domain.Entities;
using MovieSearchCase.Domain.Exceptions;
using MovieSearchCase.Domain.Interfaces.Services;
using MovieSearchCase.WebApi.Handlers.Movies;
using Xunit;
using ApiMovieDetails = MovieSearchCase.WebApi.Models.Movies.MovieDetails;

namespace MovieSearchCase.WebApi.Tests.Handlers;

public class GetMovieHandlerTests
{
    private readonly Mock<IMovieService> _movieServiceMock = new();

    [Fact]
    public async Task HandleShouldReturnOkWithMappedMovieWhenIdIsValid()
    {
        _movieServiceMock
            .Setup(service => service.GetDetailsAsync(268, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MovieDetails
            {
                Id = 268,
                Title = "Batman",
                Runtime = 126,
                Tagline = "Have you ever danced with the devil?",
                Genres = ["Fantasy", "Action"],
            });

        var result = await new GetMovieHandler(_movieServiceMock.Object, "268").HandleAsync(new DefaultHttpContext().Request);

        var movie = result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeOfType<ApiMovieDetails>().Subject;
        movie.Id.Should().Be(268);
        movie.Runtime.Should().Be(126);
        movie.Tagline.Should().Be("Have you ever danced with the devil?");
        movie.Genres.Should().Equal("Fantasy", "Action");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("1.5")]
    [InlineData("+268")]
    [InlineData("99999999999")]
    public async Task HandleShouldThrowInvalidRequestWhenIdIsInvalid(string? id)
    {
        var act = () => new GetMovieHandler(_movieServiceMock.Object, id).HandleAsync(new DefaultHttpContext().Request);

        await act.Should().ThrowAsync<MovieException>()
            .Where(exception => exception.ErrorCode == ErrorType.InvalidRequest
                && exception.Message == "The 'id' parameter must be a positive whole number.");
        _movieServiceMock.VerifyNoOtherCalls();
    }
}
