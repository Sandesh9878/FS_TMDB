using System.Net;
using System.Net.Http.Json;
using MovieSearchCase.Domain.Entities;
using MovieSearchCase.Domain.Exceptions;
using MovieSearchCase.Domain.Interfaces.Clients;

namespace MovieSearchCase.Infrastructure.Clients;

public class TmdbClient : ITmdbClient
{
    private readonly HttpClient _httpClient;

    public TmdbClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<Movie>> GetTrendingMoviesAsync(CancellationToken cancellationToken)
    {
        var response = await GetAsync<TmdbPagedResponse<TmdbMovie>>(
            "trending/movie/week",
            "trending movies",
            cancellationToken);

        return response.Results.Select(TmdbMovieMapper.ToDomainModel).ToList();
    }

    public async Task<PagedResult<Movie>> SearchMoviesAsync(string query, int page, CancellationToken cancellationToken)
    {
        // Escape the query parameters.
        var response = await GetAsync<TmdbPagedResponse<TmdbMovie>>(
            $"search/movie?query={Uri.EscapeDataString(query)}&page={page}",
            "search results",
            cancellationToken);

        return new PagedResult<Movie>
        {
            Page = response.Page,
            TotalPages = response.TotalResults == 0 ? 0 : response.TotalPages,
            TotalResults = response.TotalResults,
            Results = response.Results.Select(TmdbMovieMapper.ToDomainModel).ToList(),
        };
    }

    private async Task<T> GetAsync<T>(string requestUri, string description, CancellationToken cancellationToken)
        where T : class
    {
        T? response;

        try
        {
            response = await _httpClient.GetFromJsonAsync<T>(requestUri, cancellationToken);
        }
        catch (HttpRequestException exception) when (exception.StatusCode is HttpStatusCode.Unauthorized)
        {
            // Our token is wrong, so TMDB isn't down: surface as a 500 and keep the cause in the logs only.
            throw new InvalidOperationException("TMDB rejected the configured access token.", exception);
        }
        catch (HttpRequestException)
        {
            throw new MovieException(
                MovieException.ExceptionTitle,
                ErrorType.UpstreamServiceUnavailable,
                $"TMDB did not return {description}.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient.Timeout elapsed. If the caller's token fired instead, the client
            // disconnected and the cancellation is left to propagate.
            throw new MovieException(
                MovieException.ExceptionTitle,
                ErrorType.UpstreamServiceUnavailable,
                $"TMDB did not respond in time with {description}.");
        }

        return response ?? throw new MovieException(
            MovieException.ExceptionTitle,
            ErrorType.UpstreamServiceUnavailable,
            $"TMDB returned an empty {description} response.");
    }
}
