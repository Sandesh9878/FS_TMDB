# Notes

## What I built

| Priority | Item | Status |
|---|---|---|
| P0 | `GET /movies/search` with pagination, validation and contract errors | Done |
| P1 | Tests for handlers, service, TMDB client and the error middleware | Done (56 tests) |
| P2 | `GET /movies/{id}` and `GET /movies/{id}/videos` | Done |
| P3 | TV search, caching | Skipped |

I skipped P3 to spend the time on error handling and tests instead. Caching would also need decisions about how long TMDB data can be stale, and I'd want to agree on that before building it. TV search would mostly repeat the movie pipeline.

## Decisions

**Validation**
- `query`, `page` and `id` are bound as strings and validated in the handlers. With `int page`, ASP.NET's `[ApiController]` rejects `page=abc` with its own 400 shape (`errors: {...}`), so 400s would look different from each other. Now every 400 uses the contract's shape.
- `page` is parsed with `NumberStyles.None`, so `+5`, ` 5`, `1.5` and `1e2` are rejected. `page=` (empty) is treated as missing and defaults to 1.
- `id` must be a positive whole number, otherwise 400.
- The query is trimmed and URI-escaped, so `Fast & Furious` can't inject a `&page=` into the TMDB call.

**TMDB quirks handled in the client**
- When nothing matches, TMDB returns `total_pages: 1`. The contract says `0`, so the client normalises it.
- TMDB sends `tagline: ""` when there's no tagline, and `runtime: 0` when it's unknown. Both become `null`, as the contract allows.

**Error mapping**

| Situation | Status |
|---|---|
| TMDB unreachable, 5xx, 429, timeout (10s), unreadable JSON, empty body | 503 |
| TMDB 404 on `/movies/{id}` or `/movies/{id}/videos` | 404 |
| TMDB 404 on trending or search | 503 (it can only mean our base URL is wrong) |
| TMDB 401 (bad token) | 500: TMDB isn't down, our config is wrong. The cause is logged only. |
| Client disconnects mid-request | Nothing written, logged at Information |

- 500 and 503 responses never include `exception.Message`. The full exception goes to the log.
- 4xx are logged as warnings without a stack trace, and 5xx as errors.
- The HttpClient timeout is lowered from the 100s default to 10s, so the frontend gets a 503 quickly.

**Videos**
- `/movies/{id}/videos` returns every video TMDB has, not only YouTube ones. The contract says it returns "an array of videos" and that the frontend does the YouTube filtering. Filtering on the backend as well would hide data from other clients.

## Bugs found in the starter code

- **Problem details `type`/`instance` were swapped.** `about:blank` was put in `instance`, while `type` came out as an RFC link. The middleware now sets `type: "about:blank"` and `instance` to the request path. `ProblemDetailsException.Instance` is now unused; I left it because it's part of the starter's exception API.
- **Error responses had the wrong Content-Type.** The middleware set `application/problem+json`, then `WriteAsJsonAsync` overwrote it with `application/json`. A middleware unit test caught it.
- **500 responses leaked `exception.Message`** to the client.
- **`appsettings.Development.local.json` overrode environment variables**, because it was added after them. Env vars and command-line args win again now.

## Contract: unclear or what I'd change

- **404 for videos.** The contract only defines 404 for `/movies/{id}`. I also return 404 from `/movies/{id}/videos` for a movie that doesn't exist, rather than an empty array, which would look like "exists, no videos".
- **Page past the end.** `page=50` when there are 9 pages returns `200` with empty `results`, as TMDB does. The contract doesn't say; I think that's reasonable.
- **Trending vs search shapes.** Trending returns a bare array, while search returns a paged object. I'd make them consistent, but I didn't change the contract.
- **Extra error fields.** Responses also include `instance` and `traceId`. The contract shows four fields; clients should ignore extra ones.

## Known limitations

- If an exception is thrown after the response has started, the middleware's write would fail. No current endpoint streams, so I left it. The fix is to check `Response.HasStarted` and rethrow.
- `MovieException` has no inner exception, so 503 log entries don't show the underlying network error.
- There are no `WebApplicationFactory` integration tests. Routing and model binding were checked manually with Swagger and curl.
- Swagger shows `page` and `id` as strings, because of the validation choice above.
