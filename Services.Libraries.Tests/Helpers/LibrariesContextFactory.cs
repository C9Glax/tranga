using Microsoft.EntityFrameworkCore;
using Services.Libraries.Database;

namespace Services.Libraries.Tests.Helpers;

/// <summary>
/// Creates isolated <see cref="LibrariesContext"/> instances backed by a per-call temp-file Sqlite
/// database for tests, mirroring Services.Manga.Tests/Helpers/MangaContextFactory.cs. A file-based
/// Sqlite database is used (rather than the EF Core InMemory provider) so bulk operations like
/// <c>ExecuteUpdateAsync</c>/<c>ExecuteDeleteAsync</c>, used by production endpoints, are supported.
/// </summary>
public static class LibrariesContextFactory
{
    private static readonly string RootDirectory = Path.Combine(Path.GetTempPath(), "TrangaTests", "Services.Libraries.Tests");

    public static LibrariesContext Create()
    {
        Directory.CreateDirectory(RootDirectory);
        string dbPath = Path.Combine(RootDirectory, $"{Guid.NewGuid():N}.db");
        DbContextOptions<LibrariesContext> options = new DbContextOptionsBuilder<LibrariesContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        LibrariesContext context = new(options);
        context.Database.EnsureCreated();
        return context;
    }
}
