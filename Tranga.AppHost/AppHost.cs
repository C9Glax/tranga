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
IResourceBuilder<ParameterResource> downloadLanguage = builder.AddParameter("DownloadLanguage");
IResourceBuilder<ParameterResource> flaresolverrUrl = builder.AddParameter("FlaresolverrUrl");
IResourceBuilder<ParameterResource> useAuth = builder.AddParameter("UseAuth");
IResourceBuilder<ParameterResource> authSigningKey = builder.AddParameter("AuthSigningKey", secret: true);
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
    })
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "services-tasks";
        service.Networks = ["tranga"];
        service.Image = "ghcr.io/c9glax/tranga-services_tasks:external-connectors";
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
