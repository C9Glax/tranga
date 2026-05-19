using Common.Database;
using Microsoft.EntityFrameworkCore;

namespace Services.Libraries.Database;

public sealed class LibrariesContext : TrangaDbContext<LibrariesContext>
{
    internal DbSet<DbLibraryService> LibraryServices { get; init; }
    
    internal DbSet<DbMangaIdMapping> MangaMappings { get; init; }

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