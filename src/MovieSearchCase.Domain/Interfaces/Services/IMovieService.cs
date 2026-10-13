using MovieSearchCase.Domain.Entities;

namespace MovieSearchCase.Domain.Interfaces.Services;

/// <summary>
/// TMDB-backed movie lookups used by the API.
/// </summary>
public interface IMovieService
{
    Task<IReadOnlyList<Movie>> GetTrendingAsync(CancellationToken cancellationToken);

    Task<PagedResult<Movie>> SearchAsync(string query, int page, CancellationToken cancellationToken);

    Task<MovieDetails> GetDetailsAsync(int id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Video>> GetVideosAsync(int id, CancellationToken cancellationToken);
}
