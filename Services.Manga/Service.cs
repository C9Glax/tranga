using Common.Database;
using Common.Services.Events;
using Common.Settings;
using Extensions;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Manga.Features;
using Task = System.Threading.Tasks.Task;

namespace Services.Manga;

/// <summary>
/// Entry point for the Manga service, which owns manga/chapter/metadata/download-link/file APIs.
/// </summary>
public sealed class Service : Common.Services.Service
{
    /// <summary>
    /// Configures the database context, event publisher, and endpoints for the Manga service, then applies
    /// pending EF migrations (unless running in OpenAPI-documentation-only mode).
    /// </summary>
    /// <param name="args">Command-line arguments passed to the service.</param>
    public Service(string[] args) : base(args)
    {
        Builder.Services.AddDbContext<MangaContext>();

        Builder.Services.AddScoped<EventPublisher>();

        SetupWebApplication<Endpoints>("/mangas");

        if (!Constants.OpenApiDocumentationRun)
        {
            using MangaContext context = App.Services.CreateScope().ServiceProvider.GetRequiredService<MangaContext>();
            context.Database.MigrateAsync(CancellationToken.None).Wait();

            // Registers the sources installed on the Suwayomi sidecar as download extensions. Best-effort: if the
            // sidecar is off or not up yet, this is a no-op and the periodic refresh in Services.Tasks or an explicit
            // POST /suwayomi/refresh picks the sources up later.
            DownloadExtensionsCollection.RefreshSidecarExtensionsAsync(CancellationToken.None).Wait();
        }
    }

    /// <summary>The Manga service's entry point.</summary>
    /// <param name="args">Command-line arguments.</param>
    public static void Main(string[] args)
    {
        Service service = new (args);
        Task.WaitAll(service.Run());
    }
}
