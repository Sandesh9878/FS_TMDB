using System.Globalization;
using MovieSearchCase.Domain.Entities;

namespace MovieSearchCase.Infrastructure.Clients;

internal static class TmdbMovieMapper
{
    public static Movie ToDomainModel(this TmdbMovie tmdbMovie) => new()
    {
        Id = tmdbMovie.Id,
        Title = tmdbMovie.Title,
        Overview = tmdbMovie.Overview,
        PosterPath = tmdbMovie.PosterPath,
        BackdropPath = tmdbMovie.BackdropPath,
        VoteAverage = tmdbMovie.VoteAverage,
        ReleaseDate = ParseReleaseDate(tmdbMovie.ReleaseDate),
    };

    public static MovieDetails ToDomainModel(this TmdbMovieDetails tmdbMovie) => new()
    {
        Id = tmdbMovie.Id,
        Title = tmdbMovie.Title,
        Overview = tmdbMovie.Overview,
        PosterPath = tmdbMovie.PosterPath,
        BackdropPath = tmdbMovie.BackdropPath,
        VoteAverage = tmdbMovie.VoteAverage,
        ReleaseDate = ParseReleaseDate(tmdbMovie.ReleaseDate),
        Runtime = tmdbMovie.Runtime is > 0 ? tmdbMovie.Runtime : null,
        Tagline = string.IsNullOrWhiteSpace(tmdbMovie.Tagline) ? null : tmdbMovie.Tagline,
        Genres = tmdbMovie.Genres.Select(genre => genre.Name).ToList(),
    };

    public static Video ToDomainModel(this TmdbVideo tmdbVideo) => new()
    {
        Key = tmdbVideo.Key,
        Name = tmdbVideo.Name,
        Site = tmdbVideo.Site,
        Type = tmdbVideo.Type,
    };

    private static DateOnly? ParseReleaseDate(string? releaseDate) =>
        DateOnly.TryParseExact(
            releaseDate,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed)
            ? parsed
            : null;
}
