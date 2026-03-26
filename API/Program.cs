using System.Text.Json.Serialization;
using API;
using Database;
using Database.MangaContext;
using Microsoft.OpenApi;
using Npgsql;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(opts =>
{
    opts.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
}).ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddDbContext<MangaContext>(opts => opts.Configure(null), ServiceLifetime.Scoped, ServiceLifetime.Singleton);

WebApplication app = builder.Build();

try
{
    await app.Services.CreateScope().ServiceProvider.GetRequiredService<MangaContext>().ApplyMigrations();

    app.MapOpenApi();
    app.MapScalarApiReference();

    RouteGroupBuilder routeBuilder = app.MapGroup("/");
    Endpoints.AddEndpoints(routeBuilder);

    app.UseHttpsRedirection();

    app.Run();
}
catch (NpgsqlException)
{
    // probably build
}