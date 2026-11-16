using Common.Database;
using Common.Services.Events;
using Common.Settings;
using Extensions;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Manga.Features;
using Services.Manga.Helpers;
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

        // Request bodies validate themselves (IValidatableObject / DataAnnotations on the datatype): an invalid one is
        // answered with 400 and a list of problems before an endpoint runs, endpoints do not check anything themselves.
        Builder.Services.AddValidation();

        SetupWebApplication<Endpoints>("/mangas");

        if (!Constants.OpenApiDocumentationRun)
        {
            using MangaContext context = App.Services.CreateScope().ServiceProvider.GetRequiredService<MangaContext>();
            context.Database.MigrateWithLockAsync(CancellationToken.None, logger: App.Logger).Wait();

            // Runtime-configurable settings are persisted; an environment variable still takes precedence (locked).
            if (!Settings.AllowNSFWLocked
                && context.Settings.FirstOrDefaultAsync(s => s.Key == DbSetting.AllowNsfwKey).Result is { } allowNsfw
                && bool.TryParse(allowNsfw.Value, out bool allowNsfwValue))
                Settings.AllowNSFW = allowNsfwValue;

            // The user-defined sources live in this service's database; then load all download extensions.
            DownloadExtensionsCollection.RegisterProvider(CustomDownloaderStore.CreateProvider(App.Services));
            DownloadExtensionsCollection.RefreshAsync(CancellationToken.None).Wait();
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
