namespace MovieSearchCase.WebApi.Models.Movies;

public record MovieDetails : Movie
{
    public int? Runtime { get; init; }

    public string? Tagline { get; init; }

    public required IEnumerable<string> Genres { get; init; }
}
