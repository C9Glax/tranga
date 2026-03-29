using System.Text.Json.Serialization;
using API;
using Database;
using Database.MangaContext;
using Npgsql;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi().ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddDbContext<MangaContext>(opts => opts.Configure(null), ServiceLifetime.Scoped, ServiceLifetime.Singleton);

WebApplication app = builder.Build();

RouteGroupBuilder routeBuilder = app.MapGroup("/");
Endpoints.AddEndpoints(routeBuilder);

app.UseHttpsRedirection();

app.MapOpenApi();
app.MapScalarApiReference();

try
{
    await app.Services.CreateScope().ServiceProvider.GetRequiredService<MangaContext>().ApplyMigrations();
}
catch (NpgsqlException)
{
    // probably build
}
    
app.Run();
