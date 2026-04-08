using System.Text.Json.Serialization;
using Database;
using Database.MangaContext;
using Scalar.AspNetCore;
using Services.Manga.Features;

namespace Services.Manga;

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

        builder.Services.AddDbContext<MangaContext>(opts =>
            opts.Configure(DatabaseContextOptionsBuilder.DbType.Postgresql));

        builder.Services.AddCors();
        
        _app = builder.Build();

        _app.UseCors(x => x
            .AllowAnyMethod()
            .AllowAnyHeader()
            .SetIsOriginAllowed(origin => true) // allow any origin
            .AllowCredentials()); // allow credentials

        using (MangaContext context = _app.Services.CreateScope().ServiceProvider.GetRequiredService<MangaContext>())
        {
            Task.WaitAll(context.ApplyMigrations());
        }

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

    public static void Main(string[] args)
    {
        API api = new (args);
        Task.WaitAll(api.Run());
    }
}