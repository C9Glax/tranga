# Tranga Environment Variables

| ENV              | default    | behaviour                                                                                                                                                                          |
|------------------|------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| AllowNSFW        | `false`    | Allow NSFW content in search results (docker-compose/.env key: `ALLOWNSFW`)                                                                                                        |
| DownloadLanguage | `"en"`     | Language for downloaded chapters (docker-compose/.env key: `DOWNLOADLANGUAGE`)                                                                                                     | 
| MangaDirectory   | `"Manga"`  | Host path to bind-mount for downloaded Manga (docker-compose only; Covers are stored in a managed Docker volume) If you use Komga it is recommended to mount the path at `/tranga` | 
| FLARESOLVERR_URL | null       | When set, a [FlareSolverr](https://github.com/FlareSolverr/FlareSolverr) instance will handler requests that Cloudflare rejected (docker-compose/.env key: `FLARESOLVERRURL`)      |
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
| DBHost               |               | undefined             | 
| DBUser               |               | undefined             |
| DBPass               |               | undefined             |
| DBConnectionLifetime | `60`          |                       |
| DBConnectionTimeout  | `30`          |                       |
| DBCommandTimeout     |               |                       |