using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Services.Manga.Database;

/// <summary>
/// Lets <c>dotnet ef migrations</c> construct a <see cref="MangaContext"/> directly instead of building the full
/// <see cref="Service"/> host (which requires a reachable RabbitMQ and Postgres instance) - only used by EF
/// design-time tooling, never at runtime, where <see cref="MangaContext"/> is resolved through normal DI instead.
/// </summary>
public sealed class MangaContextDesignTimeFactory : IDesignTimeDbContextFactory<MangaContext>
{
    /// <summary>Creates a <see cref="MangaContext"/> with default options for design-time tooling.</summary>
    /// <param name="args">Command-line arguments passed by the EF Core tooling (unused).</param>
    /// <returns>A new <see cref="MangaContext"/> instance.</returns>
    public MangaContext CreateDbContext(string[] args)
    {
        DbContextOptions<MangaContext> options = new DbContextOptionsBuilder<MangaContext>().Options;
        return new MangaContext(options);
    }
}
