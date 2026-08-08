using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;

namespace Services.Manga.Tests.Helpers;

/// <summary>
/// Creates isolated <see cref="MangaContext"/> instances backed by a per-call temp-file Sqlite
/// database for tests. A real (file-based) relational provider is used rather than EF Core's
/// InMemory provider because production endpoints rely on <c>ExecuteUpdateAsync</c>, which the
/// InMemory provider does not support. A file (rather than a `:memory:` connection string) is used
/// so the database survives EF Core opening and closing the connection between operations.
/// </summary>
public static class MangaContextFactory
{
    private static readonly string RootDirectory = Path.Combine(Path.GetTempPath(), "TrangaTests", "Services.Manga.Tests");

    public static MangaContext Create()
    {
        Directory.CreateDirectory(RootDirectory);
        string dbPath = Path.Combine(RootDirectory, $"{Guid.NewGuid():N}.db");
        return CreateFor(dbPath, ensureCreated: true);
    }

    /// <summary>
    /// Opens a new, separately-tracked <see cref="MangaContext"/> against the same underlying
    /// database as <paramref name="context"/>. Production endpoints each run in their own
    /// DI-scoped context; a test that reuses one context across several endpoint calls can see
    /// stale change-tracker state for entities a *different* call touched only via a bulk
    /// <c>ExecuteUpdateAsync</c> (which bypasses the tracker) - reopening avoids that pitfall the
    /// same way a fresh HTTP request would.
    /// </summary>
    public static MangaContext Reopen(MangaContext context) =>
        CreateFor(context.Database.GetDbConnection().DataSource, ensureCreated: false);

    private static MangaContext CreateFor(string dbPath, bool ensureCreated)
    {
        DbContextOptions<MangaContext> options = new DbContextOptionsBuilder<MangaContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        MangaContext context = new(options);
        if (ensureCreated)
            context.Database.EnsureCreated();
        return context;
    }
}
