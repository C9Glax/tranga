using Common.Database;
using Microsoft.EntityFrameworkCore;

namespace Services.Libraries.Database;

public sealed class LibrariesContext : TrangaDbContext<LibrariesContext>
{
    internal DbSet<DbLibrary> Libraries { get; init; }
    
    internal DbSet<DbServiceDirectoryMapping> DirectoryMappings { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbLibrary>()
            .HasKey(l => l.Id);

        modelBuilder.Entity<DbServiceDirectoryMapping>()
            .HasKey(m => m.MappingId);

        modelBuilder.Entity<DbLibrary>()
            .HasMany<DbServiceDirectoryMapping>(l => l.ServiceDirectoryMappings)
            .WithOne(m => m.Library)
            .HasForeignKey(m => m.LibraryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}