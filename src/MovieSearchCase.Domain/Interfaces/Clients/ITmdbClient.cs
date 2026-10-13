using MovieSearchCase.Domain.Entities;

namespace MovieSearchCase.Domain.Interfaces.Clients;

/// <summary>
/// Talks to The Movie Database (TMDB) REST API. See https://developer.themoviedb.org/reference.
/// </summary>
public interface ITmdbClient
{
    /// <summary>
    /// Returns this week's trending movies (TMDB's /trending/movie/week endpoint).
    /// </summary>
    Task<IReadOnlyList<Movie>> GetTrendingMoviesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Searches movies by title (TMDB's /search/movie endpoint).
    /// </summary>
    Task<PagedResult<Movie>> SearchMoviesAsync(string query, int page, CancellationToken cancellationToken);

    /// <summary>
    /// Returns one movie with runtime, tagline and genres (TMDB's /movie/{id} endpoint).
    /// </summary>
    Task<MovieDetails> GetMovieDetailsAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Returns a movie's trailers, clips and other videos (TMDB's /movie/{id}/videos endpoint).
    /// </summary>
    Task<IReadOnlyList<Video>> GetMovieVideosAsync(int id, CancellationToken cancellationToken);
}
