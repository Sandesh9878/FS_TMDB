namespace MovieSearchCase.WebApi.Models.Movies;

public record Video
{
    public required string Key { get; init; }

    public required string Name { get; init; }

    public required string Site { get; init; }

    public required string Type { get; init; }
}
