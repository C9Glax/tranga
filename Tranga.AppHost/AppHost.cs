using Tranga.AppHost;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<ParameterResource> postgresUser = builder.AddParameter("PostgresUser");
IResourceBuilder<ParameterResource> postgresPassword = builder.AddParameter("PostgresPassword", secret: true);

IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres(EnvVars.POSTGRES_HOST, postgresUser, postgresPassword);
IResourceBuilder<PostgresDatabaseResource> db = postgres.AddDatabase(EnvVars.DBName);

IResourceBuilder<ProjectResource> mangaService = builder.AddProject<Projects.Services_Manga>("services-manga")
    .WithEndpoint(
        endpointName: "manga",
        callback: static endpoint =>
        {
            endpoint.Port = 17001;
            endpoint.UriScheme = "http";
            endpoint.Transport = "http";
        })
    .WaitFor(db)
    .WithReference(db)
    .WithEnvironment(context =>
    {
        context.EnvironmentVariables["POSTGRES_HOST"] = postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.Host);
        context.EnvironmentVariables["POSTGRES_PORT"] = postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.Port);
        context.EnvironmentVariables["POSTGRES_USER"] = postgres.Resource.UserNameParameter;
        context.EnvironmentVariables["POSTGRES_PASSWORD"] = postgres.Resource.PasswordParameter;
        context.EnvironmentVariables["POSTGRES_DATABASE"] = db.Resource.DatabaseName;
    });

IResourceBuilder<ProjectResource> tasksService = builder.AddProject<Projects.Services_Tasks>("services-tasks")
    .WithEndpoint(
        endpointName: "tasks",
        callback: static endpoint =>
        {
            endpoint.Port = 17002;
            endpoint.UriScheme = "http";
            endpoint.Transport = "http";
        })
    .WaitFor(db)
    .WithReference(db)
    .WithEnvironment(context =>
    {
        context.EnvironmentVariables["POSTGRES_HOST"] = postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.Host);
        context.EnvironmentVariables["POSTGRES_PORT"] = postgres.Resource.PrimaryEndpoint.Property(EndpointProperty.Port);
        context.EnvironmentVariables["POSTGRES_USER"] = postgres.Resource.UserNameParameter;
        context.EnvironmentVariables["POSTGRES_PASSWORD"] = postgres.Resource.PasswordParameter;
        context.EnvironmentVariables["POSTGRES_DATABASE"] = db.Resource.DatabaseName;
    });

builder.Build().Run();
