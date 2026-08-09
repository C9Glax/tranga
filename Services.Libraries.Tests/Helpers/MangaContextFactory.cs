using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;

namespace Services.Libraries.Tests.Helpers;

/// <summary>
/// Creates isolated <see cref="MangaContext"/> instances backed by a per-call temp-file Sqlite
/// database for tests, mirroring Services.Manga.Tests/Helpers/MangaContextFactory.cs. Used here so
/// <c>MangaUpdatedHandler</c> tests can seed manga metadata the same way Services.Manga does.
/// </summary>
public static class MangaContextFactory
{
    private static readonly string RootDirectory = Path.Combine(Path.GetTempPath(), "TrangaTests", "Services.Libraries.Tests.MangaContext");

    public static MangaContext Create()
    {
        Directory.CreateDirectory(RootDirectory);
        string dbPath = Path.Combine(RootDirectory, $"{Guid.NewGuid():N}.db");
        DbContextOptions<MangaContext> options = new DbContextOptionsBuilder<MangaContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        MangaContext context = new(options);
        context.Database.EnsureCreated();
        return context;
    }
}
