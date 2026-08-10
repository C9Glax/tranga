# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Big picture

Tranga is a .NET 10, multi-service manga downloader with a Nuxt frontend, a YARP gateway, Postgres, and RabbitMQ.

- `Tranga.AppHost/AppHost.cs` is the orchestration hub: it wires the Aspire stack, mirrors the Docker Compose topology, and publishes the production-like service graph (services, Postgres, RabbitMQ, YARP gateway, frontend).
- `docker-compose.yaml` is a symlink into `Tranga.AppHost/aspire-output/docker-compose.yaml`, generated from `AppHost.cs` — don't hand-edit it; regenerate via Aspire publish and keep hostnames/ports/bind mounts aligned with `AppHost.cs` instead.
- Service boundaries are deliberate:
  - `Services.Manga` owns manga/chapter/metadata/download-link/file APIs (the persistent-information service).
  - `Services.Tasks` owns queueing + workers + periodic jobs (automated tasks like fetching chapters and updating metadata).
  - `Services.Notifications` owns notification extensions (Naprise-based: Gotify, Telegram, Discord, Ntfy.sh, ...).
  - `Services.Libraries` owns library extensions (Komga today).
- `Extensions` is a shared library of third-party integrations (MangaDex, MangaUpdates, Komga, ...) consumed by the services above — it is not itself a runtime service.
- `Frontend` is a Nuxt 4 / Nuxt UI / Tailwind v4 SPA, served behind the YARP gateway alongside the API services.

## Developer workflow

- Use a git worktree and a dedicated branch for code changes rather than editing the shared checkout directly.
- Full test suite: `dotnet test Tranga.sln`
- Single test project: `dotnet test <Project>.Tests/<Project>.Tests.csproj`
- Single test: `dotnet test --filter "FullyQualifiedName~Namespace.ClassName.MethodName"`
- Run a single service in isolation: `dotnet run --project <Service>/<Service>.csproj`
- Run the integrated dev stack (Aspire, wires up Postgres/RabbitMQ/all services/frontend/gateway): `dotnet run --project Tranga.AppHost/Tranga.AppHost.csproj`
- Run the Docker Compose mirror: `docker compose up`
- Tests are xUnit v3; `Common.Tests/TrangaTest.cs` provides a shared `ct` (CancellationToken) base class other test classes derive from.
- After making changes, first check whether existing tests already cover the behavior; if not, add or update tests in the matching `*.Tests` project. Always run tests related to the changed endpoint/feature before handing off.
- When adding tests, mirror the source file name in the matching `*.Tests` project (e.g. `GetTaskEndpoint.cs` -> `GetTaskEndpointTests.cs`).
- Frontend (`cd Frontend`): `npm run dev` / `build` / `lint` / `typecheck` / `prettier`. Whenever backend endpoint/DTO shapes change, run `npm run openapi-ts` to regenerate typed API clients from `app/api/config.ts` — do not hand-edit the generated output.
- Before committing frontend changes, run `npm run typecheck`, `npm run lint`, and `npm run prettier` in `Frontend/` and make sure they pass.
- Regenerate NSwag extension clients from `GeneratedExtensionClients/Program.cs`; never hand-edit files under `GeneratedExtensionClients/GeneratedClients/`.
- Avoid editing `bin/`, `obj/`, and other generated build/client output unless the task is specifically about build artifacts or code generation.

## Startup and runtime flow

- Every service entrypoint (`Service.cs` in each `Services.*` project) derives from `Common.Services.Service`, which builds the `WebApplicationBuilder`, configures CORS/OpenAPI/Scalar, and registers RabbitMQ.
- Each concrete service calls `SetupWebApplication<Endpoints>("/prefix")`, then runs EF migrations — unless `Common.Settings.Constants.OpenApiDocumentationRun` is true.
- That docs-only mode matters: RabbitMQ registration, event handlers, and DB migrations are all skipped when the entry assembly is `GetDocument.Insider` (i.e. during OpenAPI spec generation), since no real infra is available in that context.
- `Common.Database.TrangaDbContext<T>` and `DatabaseContextOptionsBuilder` centralize PostgreSQL connection setup, sourced from `Common.Settings.EnvVars`.
- Persistent storage is intentionally volume-backed: `Mangas` for downloaded chapters (owned by `Services.Tasks`), `Covers` for cover art (owned by `Services.Manga`).

## API and endpoint conventions

- Endpoint trees live under `Features/*/Endpoints.cs`, built with ASP.NET minimal APIs plus `RouteGroupBuilder` extension methods (see `Services.Manga/Features/Endpoints.cs` for the canonical pattern: nested `MapGroup` calls with `.WithTags(...)`, one static handler class per endpoint).
- Keep route prefixes in sync with the YARP gateway routes defined in `Tranga.AppHost/AppHost.cs`: `/api/mangas`, `/api/tasks`, `/api/notifications`, `/api/libraries`.

## Events, jobs, and persistence

- RabbitMQ is the cross-service message bus. Services that react to chapter/download events register `EventPublisher` and typed handlers around `RabbitMQ.Client.IChannel` (see `Common.Services/Events/`, and `EventHandlers/` folders per service, e.g. `Services.Libraries/EventHandlers/ChapterDownloadedHandler.cs`).
- `Services.Tasks` is the only place that seeds recurring work at startup — `DbFileCleanupTask`, `MissingChapterScanTask`, `PeriodicMangaChapterFetcherTask` (in `Services.Tasks/Tasks/`), scheduled via `WorkerLogic/PeriodicTaskScheduler.cs` and run through `WorkerLogic/TaskQueue.cs` / `TaskWorker.cs`.

## Extensions system

An _Extension_ is an interface to an external service that can provide metadata, download manga, or both. Each extension has a unique `Identifier` (Guid).

- `Extensions/IExtension.cs` is the base interface; `IDownloadExtension`, `IMetadataExtension`, `ILibraryExtension`, `INotificationExtension` extend it for each capability.
- Concrete extensions live in `Extensions/Extensions/` (e.g. `MangaDex.cs` implements download + metadata, `MangaUpdates.cs` implements metadata only).
- Register new extensions in `Extensions/DownloadExtensionsCollection.cs` and/or `Extensions/MetadataExtensionsCollection.cs` depending on capability.
- If an OpenAPI definition exists for the target service, generate a client first via `GeneratedExtensionClients` (`NswagGenerator.cs` / `Program.cs`) before writing the extension.
- Add tests in `Extensions.Tests/Extensions/`, extending the shared patterns in `Extensions.Tests/DownloadExtensionTests.cs` (for `IDownloadExtension`) or `Extensions.Tests/ExtensionTests.cs` (for `IMetadataExtension`).

## Repo-specific conventions

- Nullable reference types and implicit usings are enabled solution-wide.
- Use explicit types for local variables instead of `var` — this is a deliberate codestyle choice for readability (see `CONTRIBUTING.md`).
- Environment variable names are documented in `EnvVars.md` and implemented in `Common/Settings/EnvVars.cs` plus `Tranga.AppHost/EnvVars.cs` — keep all three in sync when adding config.
- Prefer changing shared behavior in `Common.*` (`Common`, `Common.Database`, `Common.Services`) or `Tranga.ServiceDefaults` instead of duplicating bootstrapping logic inside individual services.
- Domain models are C# records for value-type/init-only semantics; test patterns for records, API CRUD endpoints, and other shared conventions are catalogued in `SHARED_TEST_PATTERNS.md`.
