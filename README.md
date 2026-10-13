# Movie Search — Backend (.NET API)

This is the backend part of the DEPT Full-Stack Coding Challenge (Movie Search).

The API is a .NET 10 layered project:

- `src/MovieSearchCase.Domain`
- `src/MovieSearchCase.Infrastructure`
- `src/MovieSearchCase.Shared`
- `src/MovieSearchCase.WebApi`

It calls [The Movie Database (TMDB)](https://www.themoviedb.org/). Today it serves this week's trending movies.

A separate frontend developer builds the UI against the [API contract](#api-contract) below, using a mock API.
Your API must follow the same contract, so the frontend works with it without any changes.

## Your tasks

### P0: core task (do this first)

1. **Search endpoint**: add `GET /api/v1/movies/search?query=&page=`.
   It proxies the TMDB `/search/movie` endpoint.
   Follow the pattern of the existing trending endpoint:
   - `src/MovieSearchCase.WebApi/Controllers/V1/MoviesController.cs`
   - `src/MovieSearchCase.WebApi/Handlers/Movies/`
   - `src/MovieSearchCase.Infrastructure/Clients/TmdbClient.cs`

   Every relevant file has a `TODO(candidate)` comment that marks where to start.
2. **Pagination**: return the page, total pages and total results in the shape the contract defines.
3. **Validation and errors**: reject invalid parameters with `400`.
   Return the error codes the contract lists.

### P1: expected once P0 works

- **Tests** for the new handler, service and client, following the existing tests in `tests/`.

### P2: if you have time

- `GET /movies/{id}` and `GET /movies/{id}/videos`, as defined in the contract.
  TMDB exposes trailer video keys through [`GET /movie/{id}/videos`](https://developer.themoviedb.org/reference/movie-videos).
  The same TMDB token covers it.

### P3: stretch

- TV/series search next to movies (TMDB has a parallel `/tv` namespace).
- Caching or performance work on search.

### Guidelines

- Reuse the handler/factory pattern and the mapper conventions.
- Do not restructure the project.
- Do not change the contract. If you think it is wrong, explain why in your notes.
- No database, no auth, no deployment.

## Using AI

You may use AI tools. They are a normal part of the job here, so use them if that is how you work.
We assess how you build the solution and how you reason about it.
Make sure your submission shows your own judgment and understanding.
Do not submit generated output that you have not reviewed.

## API contract

The frontend and backend are built separately. Both sides must follow this contract exactly.
Do not change field names or shapes.

- Base URL: `http://localhost:5080/api/v1`
- Field names use camelCase.
- Dates use the format `YYYY-MM-DD`.
- Fields marked `| null` can be `null`.

### Movie object

All movie endpoints use this shape. It is the same shape the trending endpoint returns today.

```json
{
  "id": 268,
  "title": "Batman",
  "overview": "Batman must face his most ruthless nemesis…",
  "posterPath": "/cij4dd21v2Rk2YtUQbV5kW69WB2.jpg",
  "backdropPath": "/2va32apQP97gvUxaMnL5wYt4CRB.jpg",
  "voteAverage": 7.2,
  "releaseDate": "1989-06-21"
}
```

`overview`, `posterPath`, `backdropPath` and `releaseDate` are `string | null`.

### `GET /movies/trending` (already built)

Returns an array of movie objects: `Movie[]`.

### `GET /movies/search?query={query}&page={page}` (P0)

| Parameter | Required | Rules |
|---|---|---|
| `query` | yes | Not empty and not only spaces. |
| `page` | no | Whole number from 1 to 500. Default is 1. |

Response `200 OK`:

```json
{
  "page": 1,
  "totalPages": 12,
  "totalResults": 231,
  "results": [ /* Movie objects, up to 20 per page */ ]
}
```

When nothing matches, the response is still `200 OK`.
It has `"totalPages": 0`, `"totalResults": 0` and `"results": []`.

### `GET /movies/{id}` (P2)

Response `200 OK`: a movie object with three extra fields.

```json
{
  "id": 268,
  "title": "Batman",
  "overview": "…",
  "posterPath": "…",
  "backdropPath": "…",
  "voteAverage": 7.2,
  "releaseDate": "1989-06-21",
  "runtime": 126,
  "tagline": "Have you ever danced with the devil in the pale moonlight?",
  "genres": ["Fantasy", "Action", "Crime"]
}
```

`runtime` is in minutes and is `number | null`. `tagline` is `string | null`.
If the movie does not exist, the response is `404 Not Found`.

### `GET /movies/{id}/videos` (P2)

Response `200 OK`: an array of videos. It is empty when the movie has no videos.

```json
[
  {
    "key": "dgC9Q0uhX70",
    "name": "Official Trailer",
    "site": "YouTube",
    "type": "Trailer"
  }
]
```

The frontend embeds a YouTube video with `https://www.youtube.com/embed/{key}`.
It only uses videos where `site` is `"YouTube"`.

### Errors

Every error uses the existing `application/problem+json` format:

```json
{
  "type": "about:blank",
  "title": "Invalid request",
  "status": 400,
  "detail": "The 'query' parameter is required."
}
```

| Status | When |
|---|---|
| `400 Bad Request` | A parameter breaks the rules above. |
| `404 Not Found` | The movie does not exist. |
| `503 Service Unavailable` | TMDB is down or did not respond. |
| `500 Internal Server Error` | Any other failure. |

## Getting started

### 1. Get a free TMDB API key

Sign up at <https://www.themoviedb.org/signup>.
Then generate an **API Read Access Token (v4 auth)** at <https://developer.themoviedb.org/docs/getting-started>.

### 2. Run the API

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
cp src/MovieSearchCase.WebApi/appsettings.Development.example.json \
   src/MovieSearchCase.WebApi/appsettings.Development.local.json
# edit appsettings.Development.local.json and paste in your TMDB access token
dotnet run --project src/MovieSearchCase.WebApi
```

The API runs at `http://localhost:5080`. Swagger UI is at `/swagger`.
Use Swagger to check that each endpoint matches the contract.

## Tests

```bash
dotnet test
```

## Submitting

Include a short note on:

- What you prioritized and what you skipped, and why.
- Any part of the contract you found unclear or would change, and why.
