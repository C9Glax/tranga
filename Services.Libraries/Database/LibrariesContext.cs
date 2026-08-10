using Common.Database;
using Microsoft.EntityFrameworkCore;

namespace Services.Libraries.Database;

/// <summary>
/// EF Core database context for the Libraries service, storing configured library-extension connections
/// (<see cref="DbLibraryService"/>) and their Manga-to-series mappings (<see cref="DbMangaIdMapping"/>).
/// </summary>
public sealed class LibrariesContext(DbContextOptions<LibrariesContext> options) : TrangaDbContext<LibrariesContext>(options)
{
    internal DbSet<DbLibraryService> LibraryServices { get; init; }

    internal DbSet<DbMangaIdMapping> MangaMappings { get; init; }

    /// <summary>
    /// Configures entity keys, indexes, and relationships for <see cref="DbLibraryService"/> and
    /// <see cref="DbMangaIdMapping"/>.
    /// </summary>
    /// <param name="modelBuilder">Builder used to configure the EF Core model.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbLibraryService>()
            .HasKey(l => l.LibraryServiceId);

        modelBuilder.Entity<DbMangaIdMapping>()
            .HasKey(m => new { LibraryId = m.LibraryServiceId, m.MangaId });

        modelBuilder.Entity<DbMangaIdMapping>()
            .HasIndex(m => new { LibraryId = m.LibraryServiceId, m.SeriesId });

        modelBuilder.Entity<DbLibraryService>()
            .HasMany<DbMangaIdMapping>()
            .WithOne(m => m.LibraryService)
            .HasForeignKey(m => m.LibraryServiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}