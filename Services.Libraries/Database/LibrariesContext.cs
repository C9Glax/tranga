using Common.Database;
using Microsoft.EntityFrameworkCore;

namespace Services.Libraries.Database;

public sealed class LibrariesContext : TrangaDbContext<LibrariesContext>
{
    internal DbSet<DbLibrary> Libraries { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbLibrary>()
            .HasKey(l => l.Id);
    }
}