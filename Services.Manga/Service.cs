using Common.Database;
using Common.Services.Events;
using Common.Settings;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Manga.Features;
using Task = System.Threading.Tasks.Task;

namespace Services.Manga;

public sealed class Service : Common.Services.Service
{
    public Service(string[] args) : base(args)
    {
        Builder.Services.AddDbContext<MangaContext>();

        Builder.Services.AddScoped<EventPublisher>();

        SetupWebApplication<Endpoints>("/mangas");

        if (!Constants.OpenApiDocumentationRun)
        {
            using MangaContext context = App.Services.CreateScope().ServiceProvider.GetRequiredService<MangaContext>();
            context.Database.MigrateAsync(CancellationToken.None).Wait();
        }
    }

    public static void Main(string[] args)
    {
        Service service = new (args);
        Task.WaitAll(service.Run());
    }
}