using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Services.Manga.Database;

namespace Services.Tasks.Tests.Helpers;

/// <summary>
/// Creates isolated <see cref="MangaContext"/> instances backed by a per-call temp-file Sqlite database for
/// tests, mirroring <c>Services.Manga.Tests/Helpers/MangaContextFactory.cs</c> (Services.Tasks.Tests doesn't
/// reference that project, hence the local copy) and this project's own <see cref="TasksContextFactory"/>.
/// </summary>
internal static class MangaContextFactory
{
    private static readonly string RootDirectory = Path.Combine(Path.GetTempPath(), "TrangaTests", "Services.Tasks.Tests");

    internal static MangaContext Create()
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

    /// <summary>
    /// Builds a <see cref="IServiceProvider"/> with a <see cref="MangaContext"/> registered (scoped) against a fresh
    /// temp-file Sqlite database whose schema is already created. For tests that resolve the context from a scope.
    /// </summary>
    internal static IServiceProvider CreateServices()
    {
        Directory.CreateDirectory(RootDirectory);
        string dbPath = Path.Combine(RootDirectory, $"{Guid.NewGuid():N}.db");

        ServiceCollection services = new();
        services.AddDbContext<MangaContext>(options => options.UseSqlite($"Data Source={dbPath}"));
        ServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<MangaContext>().Database.EnsureCreated();

        return provider;
    }
}
