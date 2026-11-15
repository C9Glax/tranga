# Tranga Environment Variables

| ENV              | default    | behaviour                                                                                                                                                                          |
|------------------|------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| AllowNSFW        | null       | Allow NSFW content in search results (docker-compose/.env key: `ALLOWNSFW`). When set to `true` or `false` this is final and the option is disabled in the frontend (Settings → Behavior); when empty/unset it defaults to `false` and can be changed in the frontend |
| DownloadLanguage | `"en"`     | Language for downloaded chapters (docker-compose/.env key: `DOWNLOADLANGUAGE`)                                                                                                     | 
| ChapterNamingScheme | `"?V(Vol. %V ) Ch. %C?T( - %T)"` | Template used to name downloaded chapter files: `%V` volume, `%C` chapter, `%T` title; `?X(...)` includes the text in parentheses only if that value exists |
| MangaDirectory   | `"Manga"`  | Host path to bind-mount for downloaded Manga (docker-compose only; Covers are stored in a managed Docker volume) If you use Komga it is recommended to mount the path at `/tranga` | 
| PUID              | `1000`     | User ID that owns files written into `MangaDirectory` (docker-compose only). Set this to your host user's UID (`id -u`) so files are readable/writable outside the container, and match Komga's own `PUID` if Komga also needs write access to the same directory. If `MangaDirectory` doesn't already exist on the host, Docker creates it owned by `root` on first `up` — `chown` it to `PUID:PGID` (or pre-create it yourself) before the container can write to it. |
| PGID              | `1000`     | Group ID that owns files written into `MangaDirectory` (docker-compose only); see `PUID` |
| FLARESOLVERR_URL | null       | When set, a [FlareSolverr](https://github.com/FlareSolverr/FlareSolverr) instance will handler requests that Cloudflare rejected (docker-compose/.env key: `FLARESOLVERRURL`).      |
| MAL_CLIENT_ID    | null       | Client-ID for the MyAnimeList metadata provider. Register an application at [myanimelist.net/apiconfig](https://myanimelist.net/apiconfig) (App Type "other", any redirect URL) and paste the generated Client ID here. MyAnimeList's API rejects every unauthenticated request, so while this is unset the MyAnimeList provider is not enabled at all and does not appear in the list of metadata providers; all other providers are unaffected (docker-compose/.env key: `MALCLIENTID`) |
| UseAuth          | `false`    | When set to `true`, all services require a valid credential (frontend login or an API key) on every request except health checks, OpenAPI/Scalar docs, and `/auth/status`, `/auth/setup`, `/auth/login`. On first enable, the frontend prompts to create the one admin password. Requires `AUTH_SIGNING_KEY` to also be set (docker-compose/.env key: `USEAUTH`) |
| AUTH_SIGNING_KEY | null       | Shared secret used to sign/verify the JWTs issued at login. Required when `UseAuth` is `true`; must be identical across every service. Rotating it invalidates all outstanding login sessions (API keys are unaffected, since they're validated by a database lookup, not by this key) (docker-compose/.env key: `AUTHSIGNINGKEY`) |

Generate a random value for `AUTH_SIGNING_KEY`:

- Linux/macOS (bash):
  ```bash
  openssl rand -base64 32
  ```
- Windows (PowerShell):
  ```powershell
  $b = New-Object byte[] 32; [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); [Convert]::ToBase64String($b)
  ```

## Debug

**Change these only if you know what you are doing**

| ENV             | default                | behaviour                                                |
|-----------------|------------------------|----------------------------------------------------------|
| SETTINGS_FILE   | `"settings.json"`      | Location of settings file                                |
| WORKERS_MIN     | `1`                    | Minimum number of Task workers kept running at all times |
| WORKERS_MAX     | `{ProcessorCount / 2}` | Maximum number of Task workers the pool may scale up to  |

### Database

| ENV                  | default       | behaviour             |
|----------------------|---------------|-----------------------|
| POSTGRES_HOST        | `"tranga-pg"` | Postgres server uri   |
| POSTGRES_PORT        | `5432`        | Postgres server port  |
| POSTGRES_USER        | `"postgres"`  | Postgres user         |
| POSTGRES_PASSWORD    | `"postgres"`  | Postgres password     |
| DBName               | `"tranga"`    | Database name         |
| DBHost               |               | Overrides `POSTGRES_HOST` if set |
| DBUser               |               | Overrides `POSTGRES_USER` if set |
| DBPass               |               | Overrides `POSTGRES_PASSWORD` if set |
| DBConnectionLifetime | `60`          | Connection lifetime (seconds) |
| DBConnectionTimeout  | `30`          | Connection timeout (seconds)  |
| DBCommandTimeout     | `60`          | Command timeout (seconds)     |