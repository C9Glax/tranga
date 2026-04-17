using Aspire.Hosting.Docker.Resources.ComposeNodes;
using Aspire.Hosting.Docker.Resources.ServiceNodes;
using Aspire.Hosting.JavaScript;
using Aspire.Hosting.Yarp;
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

IResourceBuilder<ProjectResource> mangaService = builder.AddProject<Services_Manga>("services-manga")
    .WaitFor(db)
    .WithReference(db)
    .WithEnvironment(context =>
    {
        context.EnvironmentVariables["POSTGRES_HOST"] = postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.Host);
        context.EnvironmentVariables["POSTGRES_PORT"] = postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.Port);
        context.EnvironmentVariables["POSTGRES_USER"] = postgres.Resource.UserNameParameter;
        context.EnvironmentVariables["POSTGRES_PASSWORD"] = postgres.Resource.PasswordParameter;
        context.EnvironmentVariables["POSTGRES_DATABASE"] = db.Resource.DatabaseName;
    })
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "services-manga";
        service.Networks = ["tranga"];
        service.Volumes.Add(new Volume()
        {
            Name = "Covers",
            Source = "Covers",
            Target = "/app/Covers",
            Type = "bind"
        });
    })
    .WithDockerfileBaseImage("mcr.microsoft.com/dotnet/sdk:10.0", "mcr.microsoft.com/dotnet/aspnet:10.0");

IResourceBuilder<ProjectResource> tasksService = builder.AddProject<Services_Tasks>("services-tasks")
    .WaitFor(db)
    .WithReference(db)
    .WithEnvironment(context =>
    {
        context.EnvironmentVariables["POSTGRES_HOST"] =
            postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.Host);
        context.EnvironmentVariables["POSTGRES_PORT"] =
            postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.Port);
        context.EnvironmentVariables["POSTGRES_USER"] = postgres.Resource.UserNameParameter;
        context.EnvironmentVariables["POSTGRES_PASSWORD"] = postgres.Resource.PasswordParameter;
        context.EnvironmentVariables["POSTGRES_DATABASE"] = db.Resource.DatabaseName;
    })
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "services-tasks";
        service.Networks = ["tranga"];
    })
    .WithDockerfileBaseImage("mcr.microsoft.com/dotnet/sdk:10.0", "mcr.microsoft.com/dotnet/aspnet:10.0");

IResourceBuilder<JavaScriptAppResource> frontend = builder.AddJavaScriptApp("frontend", "../Frontend")
    .WithEnvironment(context =>
    {
        context.EnvironmentVariables["NUXT_PUBLIC_API_BASE_URL"] = $"localhost:{port}";
    })
    .WithHttpEndpoint(port: 3000, env: "PORT")
    .WithReference(mangaService)
    .WithReference(tasksService)
    .WaitFor(mangaService)
    .WaitFor(tasksService)
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "frontend";
        service.Networks = ["tranga"];
    });

builder.AddYarp("gateway")
    .WithConfiguration(yarp =>
    {
        // Add catch-all route for frontend service
        yarp.AddRoute(frontend).WithMatchMethods("GET");

        yarp.AddRoute("/mangas/{**catch-all}", mangaService);
        yarp.AddRoute("/tasks/{**catch-all}", tasksService);
    })
    .WithHostPort(port)
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "gateway";
        service.Networks = ["tranga"];
        service.Ports = [$"{port}:{port}"];
    });

builder.Build().Run();
