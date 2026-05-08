# Tranga Environment Variables

| ENV              | default | behaviour                            |
|------------------|---------|--------------------------------------|
| AllowNSFW        | false   | Allow NSFW content in search results |
| DownloadLanguage | "en"    | Language for downloaded chapters     | 

## Debug

**Change these only if you know what you are doing**

| ENV             | default                | behaviour                 |
|-----------------|------------------------|---------------------------|
| SETTINGS_FILE   | "settings.json"        | Location of settings file |
| WORKERS_COUNT   | `{ProcessorCount / 2}` |                           |

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