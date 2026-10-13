namespace MovieSearchCase.Domain.Entities;

public class PagedResult<T>
{
    public required int Page { get; init; }

    public required int TotalPages { get; init; }

    public required int TotalResults { get; init; }

    public required IReadOnlyList<T> Results { get; init; }
}
