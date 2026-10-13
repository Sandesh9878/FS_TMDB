namespace MovieSearchCase.Domain.Entities;

public class MovieDetails : Movie
{
    public int? Runtime { get; init; }

    public string? Tagline { get; init; }

    public required IReadOnlyList<string> Genres { get; init; }
}
