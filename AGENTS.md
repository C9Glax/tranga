# AGENTS.md

## Big picture
- Tranga is a .NET 10, multi-service manga downloader with a Nuxt frontend, a YARP gateway, Postgres, and RabbitMQ.
- `Tranga.AppHost/AppHost.cs` is the orchestration hub: it wires the Aspire stack, mirrors the Docker Compose topology, and publishes the production-like service graph.
- `docker-compose.yaml` is the deployment mirror; keep hostnames, port names, and bind mounts aligned with `Tranga.AppHost/AppHost.cs`.
- Service boundaries are deliberate: `Services.Manga` owns manga/chapter/metadata/download-link/file APIs, `Services.Tasks` owns queueing + workers + periodic jobs, `Services.Notifications` owns notification extensions, and `Services.Libraries` owns library extensions (Komga today).

## Startup and runtime flow
- Most service entrypoints derive from `Common.Services.Service`, which creates the web app, configures CORS/OpenAPI/Scalar, and optionally adds RabbitMQ.
- Each concrete service calls `SetupWebApplication<Endpoints>("/prefix")`, then runs EF migrations unless `Common.Settings.Constants.OpenApiDocumentationRun` is true.
- That docs-only mode is important: RabbitMQ registration, event handlers, and migrations are skipped when the entry assembly is `GetDocument.Insider`.
- `Common.Database.TrangaDbContext<T>` and `DatabaseContextOptionsBuilder` centralize PostgreSQL connection setup from `Common.Settings.EnvVars`.

## API and endpoint conventions
- Endpoint trees live under `Features/*/Endpoints.cs` and are built with minimal APIs plus `RouteGroupBuilder` extensions.
- Follow the existing pattern of nested route groups and tags, e.g. `Services.Manga/Features/Endpoints.cs` maps `/mangas`, `/chapters`, `/metadata`, `/downloadLinks`, and `/files`.
- Keep route prefixes in sync with the gateway routes: `/api/mangas`, `/api/tasks`, `/api/notifications`, `/api/libraries`.

## Events, jobs, and persistence
- RabbitMQ is the cross-service message bus; services that react to chapter/download events register `EventPublisher` and typed handlers around `RabbitMQ.Client.IChannel`.
- `Services.Tasks` is the only place that seeds recurring work at startup (`DbFileCleanupTask`, `MissingChapterScanTask`, `PeriodicMangaChapterFetcherTask`).
- Persistent storage is intentionally volume-backed: `Mangas` for downloaded chapters and `Covers` for cover art.

## Developer workflow
- Use `dotnet test Tranga.sln` for the full test suite; tests are xUnit v3 with `Common.Tests/TrangaTest.cs` providing a shared cancellation token.
- After making changes, first check whether existing tests already cover the behavior; if not, add or update tests in the matching `*.Tests` project.
- Always run the tests related to the changed endpoint or feature before handing off, especially when touching API handlers, event handlers, or service startup code.
- Run a single service with `dotnet run --project <Service>/<Service>.csproj` when debugging a service in isolation.
- Run `Tranga.AppHost` for the integrated Aspire/dev stack, or `docker compose up` for the compose mirror.
- Regenerate NSwag clients from `GeneratedExtensionClients/Program.cs`; the generated files in `GeneratedExtensionClients/GeneratedClients/` should not be hand-edited.

## Repo-specific conventions
- Nullable is enabled and implicit usings are on across the solution; preserve existing naming patterns and ReSharper suppressions where the codebase uses env-var style identifiers.
- Environment-variable names are documented in `EnvVars.md` and implemented in `Common/Settings/EnvVars.cs` plus `Tranga.AppHost/EnvVars.cs`; keep them aligned when adding config.
- Prefer changing shared behavior in `Common.*` or `Tranga.ServiceDefaults` instead of duplicating bootstrapping logic inside services.
- Avoid editing `bin/`, `obj/`, and generated client output unless the task is specifically about build artifacts or code generation.
