using System.Text.Json.Serialization;
using Database;
using Database.MangaContext;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Scalar.AspNetCore;

namespace API;

public sealed class API : IAsyncDisposable
{
    private readonly WebApplication _app;
    
    public API(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Services.AddOpenApi().ConfigureHttpJsonOptions(opts =>
        {
            opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        builder.Services.AddDbContext<MangaContext>(opts => opts.Configure(null), ServiceLifetime.Scoped, ServiceLifetime.Singleton);

        builder.Services.AddCors(opts =>
        {
            opts.AddDefaultPolicy(new CorsPolicy
            {
                IsOriginAllowed = (s => true),
                PreflightMaxAge = null,
                SupportsCredentials = false
            });
        });
        
        _app = builder.Build();

        RouteGroupBuilder routeBuilder = _app.MapGroup("/");
        Endpoints.AddEndpoints(routeBuilder);

        _app.UseHttpsRedirection();

        _app.MapOpenApi();
        _app.MapScalarApiReference();
    }

    public async Task Run(CancellationToken? ct = null)
    {
        await _app.RunAsync(ct ?? CancellationToken.None);
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}