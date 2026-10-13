namespace MovieSearchCase.WebApi.Models;

public record PagedResult<T>
{
    public required int Page { get; init; }

    public required int TotalPages { get; init; }

    public required int TotalResults { get; init; }

    public required IEnumerable<T> Results { get; init; }
}
