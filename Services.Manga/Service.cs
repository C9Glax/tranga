using Common.Database;
using Services.Manga.Database;
using Services.Manga.Features;

namespace Services.Manga;

public sealed class Service : Common.Services.Service
{
    public Service(string[] args) : base(args)
    {
        Builder.Services.AddDbContext<MangaContext>(opts =>
            opts.Configure(DatabaseContextOptionsBuilder.DbType.Postgresql));

        SetupWebApplication<Endpoints>("/mangas");

        using MangaContext context = App.Services.CreateScope().ServiceProvider.GetRequiredService<MangaContext>();
        Task.WaitAll(context.ApplyMigrations());
    }

    public static void Main(string[] args)
    {
        Service service = new (args);
        Task.WaitAll(service.Run());
    }
}