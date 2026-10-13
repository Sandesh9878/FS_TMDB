using MovieSearchCase.Shared;

namespace MovieSearchCase.WebApi.Handlers.Movies;

public interface IMoviesRequestHandlerFactory
{
    IRequestHandlerAsync GetTrendingMovies();

    IRequestHandlerAsync SearchMovies(string? query, string? page);

    IRequestHandlerAsync GetMovie(string? id);

    IRequestHandlerAsync GetMovieVideos(string? id);
}
