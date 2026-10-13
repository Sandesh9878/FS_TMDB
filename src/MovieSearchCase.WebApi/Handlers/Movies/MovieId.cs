using System.Globalization;
using MovieSearchCase.Domain.Exceptions;

namespace MovieSearchCase.WebApi.Handlers.Movies;

internal static class MovieId
{
    public static int Parse(string? id) =>
        int.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed) && parsed > 0
            ? parsed
            : throw new MovieException(
                "Invalid request",
                ErrorType.InvalidRequest,
                "The 'id' parameter must be a positive whole number.");
}
