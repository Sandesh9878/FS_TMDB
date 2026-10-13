using Microsoft.AspNetCore.Mvc;
using MovieSearchCase.Domain.Interfaces.Services;
using MovieSearchCase.Shared;
using MovieSearchCase.WebApi.Mappers;

namespace MovieSearchCase.WebApi.Handlers.Movies;

public class GetMovieVideosHandler : IRequestHandlerAsync
{
    private readonly IMovieService _movieService;
    private readonly string? _id;

    public GetMovieVideosHandler(IMovieService movieService, string? id)
    {
        _movieService = movieService;
        _id = id;
    }

    public async Task<IActionResult> HandleAsync(HttpRequest request)
    {
        var id = MovieId.Parse(_id);

        var videos = await _movieService.GetVideosAsync(id, request.HttpContext.RequestAborted);

        return new OkObjectResult(videos.Select(video => video.ToApiModel()));
    }
}
