using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MovieSearchCase.Domain.Entities;
using MovieSearchCase.Domain.Exceptions;
using MovieSearchCase.Domain.Interfaces.Services;
using MovieSearchCase.WebApi.Handlers.Movies;
using Xunit;
using ApiVideo = MovieSearchCase.WebApi.Models.Movies.Video;

namespace MovieSearchCase.WebApi.Tests.Handlers;

public class GetMovieVideosHandlerTests
{
    private readonly Mock<IMovieService> _movieServiceMock = new();

    [Fact]
    public async Task HandleShouldReturnOkWithMappedVideosWhenIdIsValid()
    {
        _movieServiceMock
            .Setup(service => service.GetVideosAsync(268, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Video { Key = "dgC9Q0uhX70", Name = "Official Trailer", Site = "YouTube", Type = "Trailer" }]);

        var result = await new GetMovieVideosHandler(_movieServiceMock.Object, "268").HandleAsync(new DefaultHttpContext().Request);

        var videos = result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeAssignableTo<IEnumerable<ApiVideo>>().Subject;
        videos.Should().ContainSingle().Which.Should().Be(
            new ApiVideo { Key = "dgC9Q0uhX70", Name = "Official Trailer", Site = "YouTube", Type = "Trailer" });
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    public async Task HandleShouldThrowInvalidRequestWhenIdIsInvalid(string id)
    {
        var act = () => new GetMovieVideosHandler(_movieServiceMock.Object, id).HandleAsync(new DefaultHttpContext().Request);

        await act.Should().ThrowAsync<MovieException>()
            .Where(exception => exception.ErrorCode == ErrorType.InvalidRequest);
        _movieServiceMock.VerifyNoOtherCalls();
    }
}
