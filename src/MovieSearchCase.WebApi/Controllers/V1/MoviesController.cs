using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using MovieSearchCase.WebApi.Application.Versioning;
using MovieSearchCase.WebApi.Handlers.Movies;
using Swashbuckle.AspNetCore.Annotations;

namespace MovieSearchCase.WebApi.Controllers.V1;

[ApiVersion("1.0")]
[VersionedRoute]
public class MoviesController : ApiControllerBase
{
    private readonly IMoviesRequestHandlerFactory _requestHandlerFactory;

    public MoviesController(IMoviesRequestHandlerFactory requestHandlerFactory)
    {
        _requestHandlerFactory = requestHandlerFactory;
    }

    /// <summary>
    /// Returns this week's TMDB-backed trending movies.
    /// </summary>
    [HttpGet("trending")]
    [SwaggerOperation(Summary = "Get trending movies", OperationId = "GetTrendingMovies")]
    [SwaggerResponse(StatusCodes.Status200OK, "This week's trending movies", typeof(IEnumerable<Models.Movies.Movie>))]
    public async Task<IActionResult> GetTrending() =>
        await _requestHandlerFactory.GetTrendingMovies().HandleAsync(Request);

    /// <summary>
    /// Searches TMDB movies by title.
    /// </summary>
    /// <param name="query">Search text.</param>
    /// <param name="page">Page number from 1 to 500. Defaults to 1.</param>
    [HttpGet("search")]
    [SwaggerOperation(Summary = "Search movies", OperationId = "SearchMovies")]
    [SwaggerResponse(StatusCodes.Status200OK, "A page of matching movies", typeof(Models.PagedResult<Models.Movies.Movie>))]
    [SwaggerResponse(StatusCodes.Status400BadRequest, "The query or page parameter is invalid", typeof(ProblemDetails))]
    public async Task<IActionResult> Search([FromQuery] string? query, [FromQuery] string? page) =>
        await _requestHandlerFactory.SearchMovies(query, page).HandleAsync(Request);
}
