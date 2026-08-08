using Microsoft.EntityFrameworkCore;
using Services.Tasks.Database;

namespace Services.Tasks.Tests.Helpers;

/// <summary>
/// Creates isolated <see cref="TasksContext"/> instances backed by a per-call temp-file Sqlite database for
/// tests, mirroring <c>Services.Manga.Tests/Helpers/MangaContextFactory.cs</c>. A real (file-based) relational
/// provider is used rather than EF Core's InMemory provider because <see cref="WorkerLogic.WorkerPool"/> relies
/// on <c>ExecuteDeleteAsync</c>, which the InMemory provider does not support.
/// </summary>
internal static class TasksContextFactory
{
    private static readonly string RootDirectory = Path.Combine(Path.GetTempPath(), "TrangaTests", "Services.Tasks.Tests");

    internal static TasksContext Create()
    {
        Directory.CreateDirectory(RootDirectory);
        string dbPath = Path.Combine(RootDirectory, $"{Guid.NewGuid():N}.db");

        DbContextOptions<TasksContext> options = new DbContextOptionsBuilder<TasksContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        TasksContext context = new(options);
        context.Database.EnsureCreated();
        return context;
    }
}
