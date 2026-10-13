using Microsoft.AspNetCore.Mvc;
using MovieSearchCase.Domain.Interfaces.Services;
using MovieSearchCase.Shared;
using MovieSearchCase.WebApi.Mappers;

namespace MovieSearchCase.WebApi.Handlers.Movies;

public class GetMovieHandler : IRequestHandlerAsync
{
    private readonly IMovieService _movieService;
    private readonly string? _id;

    public GetMovieHandler(IMovieService movieService, string? id)
    {
        _movieService = movieService;
        _id = id;
    }

    public async Task<IActionResult> HandleAsync(HttpRequest request)
    {
        var id = MovieId.Parse(_id);

        var movie = await _movieService.GetDetailsAsync(id, request.HttpContext.RequestAborted);

        return new OkObjectResult(movie.ToApiModel());
    }
}
