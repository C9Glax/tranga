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
IResourceBuilder<ParameterResource> allowPrivateSourceHosts = builder.AddParameter("AllowPrivateSourceHosts");
IResourceBuilder<ParameterResource> downloadLanguage = builder.AddParameter("DownloadLanguage");
IResourceBuilder<ParameterResource> chapterNamingScheme = builder.AddParameter("ChapterNamingScheme");
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
        context.EnvironmentVariables["ChapterNamingScheme"] = chapterNamingScheme.Resource;
        context.EnvironmentVariables["FLARESOLVERR_URL"] = flaresolverrUrl.Resource;
        context.EnvironmentVariables["MAL_CLIENT_ID"] = malClientId.Resource;
        context.EnvironmentVariables["ALLOW_PRIVATE_SOURCE_HOSTS"] = allowPrivateSourceHosts.Resource;
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
            { "messaging", new ServiceDependency(){ Condition = "service_healthy" } }
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
        context.EnvironmentVariables["ChapterNamingScheme"] = chapterNamingScheme.Resource;
        context.EnvironmentVariables["FLARESOLVERR_URL"] = flaresolverrUrl.Resource;
        context.EnvironmentVariables["MAL_CLIENT_ID"] = malClientId.Resource;
        context.EnvironmentVariables["ALLOW_PRIVATE_SOURCE_HOSTS"] = allowPrivateSourceHosts.Resource;
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
            { "messaging", new ServiceDependency(){ Condition = "service_healthy" } }
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
    // Named to match Nuxt's automatic NUXT_-prefixed env-to-runtimeConfig mapping (see nuxt.config.ts /
    // https://nuxt.com/docs/4.x/directory-structure/env), so the frontend doesn't need to read process.env itself.
    .WithEnvironment("NUXT_SCALAR_DOCS_URL", scalarDocs.GetEndpoint("http"))
    .WaitFor(mangaService)
    .WaitFor(tasksService)
    .WaitFor(scalarDocs)
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

        // Combined API docs UI (scalar-docs container). No dedicated gateway route: ASP.NET Core/YARP's path
        // matching treats "/docs" and "/docs/" as equivalent, so a route meant to catch only one of them ends up
        // stealing the other too - a real problem here, since the container's own HTML references its script
        // with a bare relative path that only resolves correctly with the trailing slash present, and a route
        // meant to redirect only the bare path would occasionally swallow the already-correct one instead.
        // /docs** already falls through to the frontend catch-all above, which proxies it to the scalar-docs
        // container itself (see Frontend/server/routes/docs.get.ts) - the raw path is available there as a
        // plain string, letting it draw that distinction reliably.

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
