using Aspire.Hosting.Docker.Resources.ComposeNodes;
using Aspire.Hosting.Docker.Resources.ServiceNodes;
using Aspire.Hosting.JavaScript;
using Aspire.Hosting.Yarp;
using Aspire.Hosting.Yarp.Transforms;
using Projects;
using EnvVars = Tranga.AppHost.EnvVars;

#pragma warning disable ASPIREDOCKERFILEBUILDER001

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// The Suwayomi sidecar runs Tachiyomi/Mihon extension APKs (the keiyoushi repository) on the JVM, which is the only
// way to reach those sources from .NET. It is opt-in: with ENABLE_SUWAYOMI unset, the container is never added and the
// services register no Suwayomi-backed extensions. Read up here because the compose volume list needs it.
IResourceBuilder<ParameterResource> enableSuwayomiParameter = builder.AddParameter("EnableSuwayomi");
bool enableSuwayomi = bool.TryParse(enableSuwayomiParameter.Resource.GetValueAsync(CancellationToken.None).Result, out bool suwayomiRequested) && suwayomiRequested;

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
        if (enableSuwayomi)
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
IResourceBuilder<ParameterResource> useAuth = builder.AddParameter("UseAuth");
IResourceBuilder<ParameterResource> authSigningKey = builder.AddParameter("AuthSigningKey", secret: true);

// Suwayomi speaks FlareSolverr natively, so it inherits whatever Tranga is configured to use. Resolved here rather
// than at container start because the compose file bakes the enabled flag in at publish time.
bool flaresolverrConfigured = !string.IsNullOrEmpty(flaresolverrUrl.Resource.GetValueAsync(CancellationToken.None).Result);

IResourceBuilder<ContainerResource>? suwayomi = enableSuwayomi
    ? builder.AddContainer("suwayomi", "ghcr.io/suwayomi/suwayomi-server", "stable")
        .WithHttpEndpoint(name: "http", port: 4567, targetPort: 4567)
        .WithEnvironment("EXTENSION_STORES", "[\"https://github.com/keiyoushi/extensions/raw/repo/index.pb\"]")
        .WithEnvironment("WEB_UI_ENABLED", "true")
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
            // docker compose cannot start a service conditionally from an arbitrary variable, so the container is
            // gated behind a profile: a compose deployment sets COMPOSE_PROFILES=suwayomi alongside ENABLE_SUWAYOMI.
            service.Profiles = ["suwayomi"];
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
        })
    : null;
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
        context.EnvironmentVariables["ENABLE_SUWAYOMI"] = enableSuwayomiParameter.Resource;
        if (suwayomi is not null)
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
            { "messaging", new ServiceDependency(){ Condition = "service_healthy" } }
        };
        service.Restart = "on-failure:3";
    })
    .WithDockerfileBaseImage("mcr.microsoft.com/dotnet/sdk:10.0", "mcr.microsoft.com/dotnet/aspnet:10.0");

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
        context.EnvironmentVariables["ENABLE_SUWAYOMI"] = enableSuwayomiParameter.Resource;
        if (suwayomi is not null)
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
            { "messaging", new ServiceDependency(){ Condition = "service_healthy" } }
        };
        service.Restart = "on-failure:3";
    })
    .WithDockerfileBaseImage("mcr.microsoft.com/dotnet/sdk:10.0", "mcr.microsoft.com/dotnet/aspnet:10.0");

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
    .WithDockerfileBaseImage("mcr.microsoft.com/dotnet/sdk:10.0", "mcr.microsoft.com/dotnet/aspnet:10.0");

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
    .WithDockerfileBaseImage("mcr.microsoft.com/dotnet/sdk:10.0", "mcr.microsoft.com/dotnet/aspnet:10.0");

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
    .WithDockerfileBaseImage("mcr.microsoft.com/dotnet/sdk:10.0", "mcr.microsoft.com/dotnet/aspnet:10.0");

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

        yarp.AddRoute("/api/mangas/{**catch-all}", mangaService).WithTransformPathRemovePrefix("/api");
        yarp.AddRoute("/api/tasks/{**catch-all}", tasksService).WithTransformPathRemovePrefix("/api");
        yarp.AddRoute("/api/notifications/{**catch-all}", notificationsService).WithTransformPathRemovePrefix("/api");
        yarp.AddRoute("/api/libraries/{**catch-all}", librariesService).WithTransformPathRemovePrefix("/api");
        yarp.AddRoute("/api/auth/{**catch-all}", authService).WithTransformPathRemovePrefix("/api");

        // Suwayomi's own WebUI, for the per-source preferences Tranga does not wrap. Its assets are built with a
        // relative base so they resolve under this prefix, but its router has no basename — link to /suwayomi/ and
        // treat deep links as unsupported. Tranga's own Settings -> Sources page is the primary way to manage
        // extensions; services reach the sidecar directly over the tranga network, not through here.
        if (suwayomi is not null)
            yarp.AddRoute("/suwayomi/{**catch-all}", suwayomi.GetEndpoint("http")).WithTransformPathRemovePrefix("/suwayomi");
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
        // Deliberately not a DependsOn: the sidecar lives behind a compose profile, and depending on a service that
        // the active profile did not start would refuse to bring the gateway up at all.
    });

builder.Build().Run();
