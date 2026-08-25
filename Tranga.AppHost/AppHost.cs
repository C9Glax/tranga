using Aspire.Hosting.Docker.Resources.ComposeNodes;
using Aspire.Hosting.Docker.Resources.ServiceNodes;
using Aspire.Hosting.JavaScript;
using Aspire.Hosting.Yarp;
using Aspire.Hosting.Yarp.Transforms;
using Projects;
using EnvVars = Tranga.AppHost.EnvVars;

#pragma warning disable ASPIREDOCKERFILEBUILDER001

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("env")
    .WithProperties(env =>
    {
        env.DashboardEnabled = false;
    })
    .ConfigureComposeFile(conf =>
    {
        conf.AddNetwork(new Network()
        {
            Name = "tranga",
            Driver = "bridge"
        });
        conf.AddVolume(new Volume()
        {
            Name = "Covers"
        });
        conf.AddVolume(new Volume()
        {
            Name = "Suwayomi"
        });
    });

IResourceBuilder<ParameterResource> postgresUser = builder.AddParameter("PostgresUser");
IResourceBuilder<ParameterResource> postgresPassword = builder.AddParameter("PostgresPassword", secret: true);
IResourceBuilder<ParameterResource> portResource = builder.AddParameter("Port");
int port = portResource.Resource.GetValueAsync(CancellationToken.None).Result is { } v ? int.Parse(v) : 5000;

IResourceBuilder<PostgresServerResource> postgres = builder
    .AddPostgres(EnvVars.POSTGRES_HOST, postgresUser, postgresPassword)
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "tranga-pg";
        service.Networks = ["tranga"];
    });
IResourceBuilder<PostgresDatabaseResource> db = postgres.AddDatabase(EnvVars.DBName);

IResourceBuilder<ParameterResource> rabbitUser = builder.AddParameter("RabbitMqUser");
IResourceBuilder<ParameterResource> rabbitPassword = builder.AddParameter("RabbitMqPassword", secret: true);

IResourceBuilder<ParameterResource> allowNsfw = builder.AddParameter("AllowNSFW");
IResourceBuilder<ParameterResource> downloadLanguage = builder.AddParameter("DownloadLanguage");
IResourceBuilder<ParameterResource> flaresolverrUrl = builder.AddParameter("FlaresolverrUrl");
IResourceBuilder<ParameterResource> malClientId = builder.AddParameter("MalClientId", secret: true);
IResourceBuilder<ParameterResource> useAuth = builder.AddParameter("UseAuth");
IResourceBuilder<ParameterResource> authSigningKey = builder.AddParameter("AuthSigningKey", secret: true);

// SixLabors.ImageSharp (pulled in transitively via Common) only warns about a missing license in Debug builds;
// `aspire publish` builds every project in Release, where the same check is a hard build failure. The plain
// project-publish path Aspire uses by default (a hardcoded `dotnet publish /t:PublishContainer` invocation) has
// no hook for extra MSBuild properties or environment variables, so each service instead publishes from its own
// existing CI Dockerfile (see e.g. Services.Manga/Dockerfile) via PublishAsDockerFile below, which already mounts
// this exact secret id for its own `dotnet build`/`publish` steps.
IResourceBuilder<ParameterResource> sixLaborsLicenseKey = builder.AddParameter("SixLaborsLicenseKey", secret: true);

// Suwayomi speaks FlareSolverr natively, so it inherits whatever Tranga is configured to use. Under `aspire run` the
// parameter is resolved now; the compose output overrides this with an interpolation so that .env stays authoritative
// there (see PublishAsDockerComposeService below).
bool flaresolverrConfigured = !string.IsNullOrEmpty(flaresolverrUrl.Resource.GetValueAsync(CancellationToken.None).Result);

// The Suwayomi sidecar runs Tachiyomi/Mihon extension APKs (the keiyoushi repository) on the JVM, which is the only
// way to reach those sources from .NET. Tranga depends on it: MangaDex aside, every download source comes from here.
IResourceBuilder<ContainerResource> suwayomi = builder.AddContainer("suwayomi", "ghcr.io/suwayomi/suwayomi-server", "stable")
    .WithHttpEndpoint(name: "http", port: 4567, targetPort: 4567)
    .WithEnvironment("EXTENSION_STORES", "[\"https://github.com/keiyoushi/extensions/raw/repo/index.pb\"]")
    // The sidecar is never reached by a browser: it is not routed through the gateway, so it would be unauthenticated
    // if it were. Tranga's own Settings -> Sources page manages extensions, so the WebUI has no reason to run.
    .WithEnvironment("WEB_UI_ENABLED", "false")
    .WithEnvironment("AUTH_MODE", "none")
    // KCEF downloads a ~500MB Chromium at first start to provide a WebView. FlareSolverr covers the Cloudflare
    // cases Tranga cares about, so it stays off.
    .WithEnvironment("KCEF_ENABLED", "false")
    .WithEnvironment("FLARESOLVERR_ENABLED", flaresolverrConfigured ? "true" : "false")
    .WithEnvironment("FLARESOLVERR_URL", flaresolverrUrl.Resource)
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "suwayomi";
        service.Networks = ["tranga"];
        // Derive the flag from FLARESOLVERRURL at container start rather than baking in whatever the parameter
        // happened to be when this file was generated, so setting FlareSolverr in .env is enough to enable it here
        // too. Compose expands ":+" to "true" only when the variable is set and non-empty; when it is empty the
        // image's startup script falls back to the config default (`${FLARESOLVERR_ENABLED:-\1}`), i.e. false.
        service.Environment["FLARESOLVERR_ENABLED"] = "${FLARESOLVERRURL:+true}";
        // Persistent state, not a cache: this volume holds the installed extension JARs as well as the manga rows
        // whose ids back url-to-id resolution. Wiping it means reinstalling every extension.
        service.Volumes.Add(new Volume()
        {
            Name = "Suwayomi",
            Source = "Suwayomi",
            Target = "/home/suwayomi/.local/share/Tachidesk",
            Type = "volume"
        });
        service.Restart = "on-failure:3";
    });

IResourceBuilder<RabbitMQServerResource> rabbitmq = builder.AddRabbitMQ("messaging", rabbitUser, rabbitPassword)
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "messaging";
        service.Networks = ["tranga"];
        service.Healthcheck = new Healthcheck
        {
            Test = ["CMD", "rabbitmq-diagnostics", "-q", "check_port_connectivity"],
            Interval = "5s",
            Timeout = "5s",
            Retries = 12,
            StartPeriod = "10s"
        };
    });

IResourceBuilder<ProjectResource> tasksService = builder.AddProject<Services_Tasks>("services-tasks")
    .WaitFor(rabbitmq)
    .WaitFor(db)
    .WithReference(db)
    .WithReference(rabbitmq)
    .WithEnvironment(context =>
    {
        context.EnvironmentVariables["POSTGRES_HOST"] = postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.Host);
        context.EnvironmentVariables["POSTGRES_PORT"] = postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.Port);
        context.EnvironmentVariables["POSTGRES_USER"] = postgres.Resource.UserNameParameter;
        context.EnvironmentVariables["POSTGRES_PASSWORD"] = postgres.Resource.PasswordParameter;
        context.EnvironmentVariables["POSTGRES_DATABASE"] = db.Resource.DatabaseName;
        context.EnvironmentVariables["RABBITMQ_HOST"] = rabbitmq.Resource.PrimaryEndpoint.Property(EndpointProperty.Host);
        context.EnvironmentVariables["RABBITMQ_PORT"] = rabbitmq.Resource.PrimaryEndpoint.Property(EndpointProperty.Port);
        context.EnvironmentVariables["RABBITMQ_USER"] = rabbitUser.Resource.GetValueAsync(CancellationToken.None).Result;
        context.EnvironmentVariables["RABBITMQ_PASSWORD"] = rabbitPassword.Resource.GetValueAsync(CancellationToken.None).Result;
        context.EnvironmentVariables["UseAuth"] = useAuth.Resource;
        context.EnvironmentVariables["AUTH_SIGNING_KEY"] = authSigningKey.Resource;
        context.EnvironmentVariables["AllowNSFW"] = allowNsfw.Resource;
        context.EnvironmentVariables["DownloadLanguage"] = downloadLanguage.Resource;
        context.EnvironmentVariables["FLARESOLVERR_URL"] = flaresolverrUrl.Resource;
        context.EnvironmentVariables["MAL_CLIENT_ID"] = malClientId.Resource;
        context.EnvironmentVariables["SUWAYOMI_URL"] = suwayomi.GetEndpoint("http");
    })
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "services-tasks";
        service.Networks = ["tranga"];
        service.Image = "ghcr.io/c9glax/tranga-services_tasks:external-connectors";
        service.User = "${PUID:-1000}:${PGID:-1000}";
        service.Volumes.Add(new Volume()
        {
            Name = "Mangas",
            Source = "${MangaDirectory}",
            Target = "/app/Mangas",
            Type = "bind"
        });
        service.DependsOn = new()
        {
            { "tranga-pg", new ServiceDependency(){ Condition = "service_started" } },
            { "messaging", new ServiceDependency(){ Condition = "service_healthy" } },
            // Only "started": the sidecar takes a while to be usable, and extension discovery is best-effort with a
            // retry, so there is no reason to hold the service back until it is ready.
            { "suwayomi", new ServiceDependency(){ Condition = "service_started" } }
        };
        service.Restart = "on-failure:3";
    })
    .PublishAsDockerFile(container => container
        .WithDockerfile("..", "Services.Tasks/Dockerfile")
        .WithBuildSecret("sixlabors_lic", sixLaborsLicenseKey));

IResourceBuilder<ProjectResource> mangaService = builder.AddProject<Services_Manga>("services-manga")
    .WaitFor(rabbitmq)
    .WaitFor(db)
    .WithReference(db)
    .WithReference(rabbitmq)
    .WithEnvironment(context =>
    {
        context.EnvironmentVariables["POSTGRES_HOST"] = postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.Host);
        context.EnvironmentVariables["POSTGRES_PORT"] = postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.Port);
        context.EnvironmentVariables["POSTGRES_USER"] = postgres.Resource.UserNameParameter;
        context.EnvironmentVariables["POSTGRES_PASSWORD"] = postgres.Resource.PasswordParameter;
        context.EnvironmentVariables["POSTGRES_DATABASE"] = db.Resource.DatabaseName;
        context.EnvironmentVariables["RABBITMQ_HOST"] = rabbitmq.Resource.PrimaryEndpoint.Property(EndpointProperty.Host);
        context.EnvironmentVariables["RABBITMQ_PORT"] = rabbitmq.Resource.PrimaryEndpoint.Property(EndpointProperty.Port);
        context.EnvironmentVariables["RABBITMQ_USER"] = rabbitUser.Resource.GetValueAsync(CancellationToken.None).Result;
        context.EnvironmentVariables["RABBITMQ_PASSWORD"] = rabbitPassword.Resource.GetValueAsync(CancellationToken.None).Result;
        context.EnvironmentVariables["UseAuth"] = useAuth.Resource;
        context.EnvironmentVariables["AUTH_SIGNING_KEY"] = authSigningKey.Resource;
        context.EnvironmentVariables["AllowNSFW"] = allowNsfw.Resource;
        context.EnvironmentVariables["DownloadLanguage"] = downloadLanguage.Resource;
        context.EnvironmentVariables["FLARESOLVERR_URL"] = flaresolverrUrl.Resource;
        context.EnvironmentVariables["MAL_CLIENT_ID"] = malClientId.Resource;
        context.EnvironmentVariables["SUWAYOMI_URL"] = suwayomi.GetEndpoint("http");
    })
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "services-manga";
        service.Networks = ["tranga"];
        service.Image = "ghcr.io/c9glax/tranga-services_manga:external-connectors";
        service.Volumes.Add(new Volume()
        {
            Name = "Covers",
            Source = "Covers",
            Target = "/app/Covers",
            Type = "volume"
        });
        service.DependsOn = new()
        {
            { "tranga-pg", new ServiceDependency(){ Condition = "service_started" } },
            { "messaging", new ServiceDependency(){ Condition = "service_healthy" } },
            // Only "started": the sidecar takes a while to be usable, and extension discovery is best-effort with a
            // retry, so there is no reason to hold the service back until it is ready.
            { "suwayomi", new ServiceDependency(){ Condition = "service_started" } }
        };
        service.Restart = "on-failure:3";
    })
    .PublishAsDockerFile(container => container
        .WithDockerfile("..", "Services.Manga/Dockerfile")
        .WithBuildSecret("sixlabors_lic", sixLaborsLicenseKey));

IResourceBuilder<ProjectResource> notificationsService = builder.AddProject<Services_Notifications>("services-notifications")
    .WaitFor(rabbitmq)
    .WaitFor(db)
    .WithReference(db)
    .WithReference(rabbitmq)
    .WithEnvironment(context =>
    {
        context.EnvironmentVariables["POSTGRES_HOST"] = postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.Host);
        context.EnvironmentVariables["POSTGRES_PORT"] = postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.Port);
        context.EnvironmentVariables["POSTGRES_USER"] = postgres.Resource.UserNameParameter;
        context.EnvironmentVariables["POSTGRES_PASSWORD"] = postgres.Resource.PasswordParameter;
        context.EnvironmentVariables["POSTGRES_DATABASE"] = db.Resource.DatabaseName;
        context.EnvironmentVariables["RABBITMQ_HOST"] = rabbitmq.Resource.PrimaryEndpoint.Property(EndpointProperty.Host);
        context.EnvironmentVariables["RABBITMQ_PORT"] = rabbitmq.Resource.PrimaryEndpoint.Property(EndpointProperty.Port);
        context.EnvironmentVariables["RABBITMQ_USER"] = rabbitUser.Resource.GetValueAsync(CancellationToken.None).Result;
        context.EnvironmentVariables["RABBITMQ_PASSWORD"] = rabbitPassword.Resource.GetValueAsync(CancellationToken.None).Result;
        context.EnvironmentVariables["UseAuth"] = useAuth.Resource;
        context.EnvironmentVariables["AUTH_SIGNING_KEY"] = authSigningKey.Resource;
    })
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "services-notifications";
        service.Networks = ["tranga"];
        service.Image = "ghcr.io/c9glax/tranga-services_notifications:external-connectors";
        service.DependsOn = new()
        {
            { "tranga-pg", new ServiceDependency(){ Condition = "service_started" } },
            { "messaging", new ServiceDependency(){ Condition = "service_healthy" } }
        };
        service.Restart = "on-failure:3";
    })
    .PublishAsDockerFile(container => container
        .WithDockerfile("..", "Services.Notifications/Dockerfile")
        .WithBuildSecret("sixlabors_lic", sixLaborsLicenseKey));

IResourceBuilder<ProjectResource> librariesService = builder.AddProject<Services_Libraries>("services-libraries")
    .WaitFor(rabbitmq)
    .WaitFor(db)
    .WithReference(db)
    .WithReference(rabbitmq)
    .WithEnvironment(context =>
    {
        context.EnvironmentVariables["POSTGRES_HOST"] = postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.Host);
        context.EnvironmentVariables["POSTGRES_PORT"] = postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.Port);
        context.EnvironmentVariables["POSTGRES_USER"] = postgres.Resource.UserNameParameter;
        context.EnvironmentVariables["POSTGRES_PASSWORD"] = postgres.Resource.PasswordParameter;
        context.EnvironmentVariables["POSTGRES_DATABASE"] = db.Resource.DatabaseName;
        context.EnvironmentVariables["RABBITMQ_HOST"] = rabbitmq.Resource.PrimaryEndpoint.Property(EndpointProperty.Host);
        context.EnvironmentVariables["RABBITMQ_PORT"] = rabbitmq.Resource.PrimaryEndpoint.Property(EndpointProperty.Port);
        context.EnvironmentVariables["RABBITMQ_USER"] = rabbitUser.Resource.GetValueAsync(CancellationToken.None).Result;
        context.EnvironmentVariables["RABBITMQ_PASSWORD"] = rabbitPassword.Resource.GetValueAsync(CancellationToken.None).Result;
        context.EnvironmentVariables["UseAuth"] = useAuth.Resource;
        context.EnvironmentVariables["AUTH_SIGNING_KEY"] = authSigningKey.Resource;
    })
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "services-libraries";
        service.Networks = ["tranga"];
        service.Image = "ghcr.io/c9glax/tranga-services_libraries:external-connectors";
        service.Volumes.Add(new Volume()
        {
            Name = "Covers",
            Source = "Covers",
            Target = "/app/Covers",
            Type = "volume",
            ReadOnly = true
        });
        service.DependsOn = new()
        {
            { "tranga-pg", new ServiceDependency(){ Condition = "service_started" } },
            { "messaging", new ServiceDependency(){ Condition = "service_healthy" } }
        };
        service.Restart = "on-failure:3";
    })
    .PublishAsDockerFile(container => container
        .WithDockerfile("..", "Services.Libraries/Dockerfile")
        .WithBuildSecret("sixlabors_lic", sixLaborsLicenseKey));

IResourceBuilder<ProjectResource> authService = builder.AddProject<Services_Auth>("services-auth")
    .WaitFor(rabbitmq)
    .WaitFor(db)
    .WithReference(db)
    .WithReference(rabbitmq)
    .WithEnvironment(context =>
    {
        context.EnvironmentVariables["POSTGRES_HOST"] = postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.Host);
        context.EnvironmentVariables["POSTGRES_PORT"] = postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.Port);
        context.EnvironmentVariables["POSTGRES_USER"] = postgres.Resource.UserNameParameter;
        context.EnvironmentVariables["POSTGRES_PASSWORD"] = postgres.Resource.PasswordParameter;
        context.EnvironmentVariables["POSTGRES_DATABASE"] = db.Resource.DatabaseName;
        context.EnvironmentVariables["RABBITMQ_HOST"] = rabbitmq.Resource.PrimaryEndpoint.Property(EndpointProperty.Host);
        context.EnvironmentVariables["RABBITMQ_PORT"] = rabbitmq.Resource.PrimaryEndpoint.Property(EndpointProperty.Port);
        context.EnvironmentVariables["RABBITMQ_USER"] = rabbitUser.Resource.GetValueAsync(CancellationToken.None).Result;
        context.EnvironmentVariables["RABBITMQ_PASSWORD"] = rabbitPassword.Resource.GetValueAsync(CancellationToken.None).Result;
        context.EnvironmentVariables["UseAuth"] = useAuth.Resource;
        context.EnvironmentVariables["AUTH_SIGNING_KEY"] = authSigningKey.Resource;
    })
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "services-auth";
        service.Networks = ["tranga"];
        service.Image = "ghcr.io/c9glax/tranga-services_auth:external-connectors";
        service.DependsOn = new()
        {
            { "tranga-pg", new ServiceDependency(){ Condition = "service_started" } },
            { "messaging", new ServiceDependency(){ Condition = "service_healthy" } }
        };
        service.Restart = "on-failure:3";
    })
    .PublishAsDockerFile(container => container
        .WithDockerfile("..", "Services.Auth/Dockerfile")
        .WithBuildSecret("sixlabors_lic", sixLaborsLicenseKey));

// Combined API docs: one Scalar instance showing every service's OpenAPI document side by side. It's a browser
// app, so the "sources" URLs must be reachable by the visitor's browser - they point at the gateway-routed
// /api/{service}/openapi/... paths (see the gateway config below), not at the services directly.
IResourceBuilder<ContainerResource> scalarDocs = builder.AddContainer("scalar-docs", "scalarapi/api-reference", "latest")
    .WithHttpEndpoint(name: "http", port: 8080, targetPort: 8080)
    .WithEnvironment("BASE_PATH", "/docs")
    .WithEnvironment("API_REFERENCE_CONFIG", """
        {"sources":[
            {"title":"Manga","slug":"manga","url":"/api/mangas/openapi/v1.json","default":true},
            {"title":"Tasks","slug":"tasks","url":"/api/tasks/openapi/v1.json"},
            {"title":"Notifications","slug":"notifications","url":"/api/notifications/openapi/v1.json"},
            {"title":"Libraries","slug":"libraries","url":"/api/libraries/openapi/v1.json"},
            {"title":"Auth","slug":"auth","url":"/api/auth/openapi/v1.json"}
        ]}
        """)
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "scalar-docs";
        service.Networks = ["tranga"];
        service.Restart = "on-failure:3";
    });

IResourceBuilder<JavaScriptAppResource> frontend = builder.AddJavaScriptApp("frontend", "../Frontend")
    .WithHttpEndpoint(port: 3000, env: "PORT")
    .WithReference(mangaService)
    .WithReference(tasksService)
    .WaitFor(mangaService)
    .WaitFor(tasksService)
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "frontend";
        service.Networks = ["tranga"];
        service.Image = "ghcr.io/c9glax/tranga-frontend:external-connectors";
        service.DependsOn = new()
        {
            { "services-manga", new ServiceDependency(){ Condition = "service_started" } },
            { "services-tasks", new ServiceDependency(){ Condition = "service_started" } }
        };
    });

builder.AddYarp("gateway")
    .WithConfiguration(yarp =>
    {
        // Add catch-all route for frontend service
        yarp.AddRoute(frontend).WithMatchMethods("GET");

        // Combined API docs UI. The container serves its page at "/" and only uses BASE_PATH to prefix the
        // asset/config URLs it generates for itself, so the gateway still needs to strip "/docs" here.
        yarp.AddRoute("/docs/{**catch-all}", scalarDocs.GetEndpoint("http")).WithTransformPathRemovePrefix("/docs");

        // Docs: each service's OpenAPI JSON is mapped at its own root (/openapi/v1.json), not under its
        // endpointsPrefix, so these routes strip the full "/api/{service}" prefix instead of just "/api" to
        // land on it. Registered before the broader catch-alls below so they take precedence.
        yarp.AddRoute("/api/mangas/openapi/{**catch-all}", mangaService).WithTransformPathRemovePrefix("/api/mangas");
        yarp.AddRoute("/api/tasks/openapi/{**catch-all}", tasksService).WithTransformPathRemovePrefix("/api/tasks");
        yarp.AddRoute("/api/notifications/openapi/{**catch-all}", notificationsService).WithTransformPathRemovePrefix("/api/notifications");
        yarp.AddRoute("/api/libraries/openapi/{**catch-all}", librariesService).WithTransformPathRemovePrefix("/api/libraries");
        yarp.AddRoute("/api/auth/openapi/{**catch-all}", authService).WithTransformPathRemovePrefix("/api/auth");

        yarp.AddRoute("/api/mangas/{**catch-all}", mangaService).WithTransformPathRemovePrefix("/api");
        yarp.AddRoute("/api/tasks/{**catch-all}", tasksService).WithTransformPathRemovePrefix("/api");
        yarp.AddRoute("/api/notifications/{**catch-all}", notificationsService).WithTransformPathRemovePrefix("/api");
        yarp.AddRoute("/api/libraries/{**catch-all}", librariesService).WithTransformPathRemovePrefix("/api");
        yarp.AddRoute("/api/auth/{**catch-all}", authService).WithTransformPathRemovePrefix("/api");

        // The Suwayomi sidecar is deliberately not routed here. The gateway does not authenticate - every other route
        // is protected by the service behind it - so exposing the sidecar would hand out unauthenticated control of
        // it. Services reach it directly over the tranga network, and its extension icons are served back through
        // the manga service.
    })
    .WithHostPort(port)
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "gateway";
        service.Networks = ["tranga"];
        service.Ports = [$"{port}:{port}"];
        service.DependsOn = new()
        {
            { "frontend", new ServiceDependency(){ Condition = "service_started" } }
        };
    });

builder.Build().Run();
