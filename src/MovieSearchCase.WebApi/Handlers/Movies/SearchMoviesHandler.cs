using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using MovieSearchCase.Domain.Exceptions;
using MovieSearchCase.Domain.Interfaces.Services;
using MovieSearchCase.Shared;
using MovieSearchCase.WebApi.Mappers;

namespace MovieSearchCase.WebApi.Handlers.Movies;

public class SearchMoviesHandler : IRequestHandlerAsync
{
    public const int MaxPage = 500;

    private const string InvalidRequestTitle = "Invalid request";

    private readonly IMovieService _movieService;
    private readonly string? _query;
    private readonly string? _page;

    public SearchMoviesHandler(IMovieService movieService, string? query, string? page)
    {
        _movieService = movieService;
        _query = query;
        _page = page;
    }

    public async Task<IActionResult> HandleAsync(HttpRequest request)
    {
        if (string.IsNullOrWhiteSpace(_query))
        {
            throw new MovieException(
                InvalidRequestTitle,
                ErrorType.InvalidRequest,
                "The 'query' parameter is required.");
        }

        var page = 1;

        if (_page is not null
            && (!int.TryParse(_page, NumberStyles.None, CultureInfo.InvariantCulture, out page)
                || page is < 1 or > MaxPage))
        {
            throw new MovieException(
                InvalidRequestTitle,
                ErrorType.InvalidRequest,
                $"The 'page' parameter must be a whole number from 1 to {MaxPage}.");
        }

        var result = await _movieService.SearchAsync(_query.Trim(), page, request.HttpContext.RequestAborted);

        return new OkObjectResult(result.ToApiModel());
    }
}
