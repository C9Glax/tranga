using API.Schema.LibraryConnectors;
using Microsoft.EntityFrameworkCore;

namespace API.Schema.Contexts;

public class LibraryContext(DbContextOptions<LibraryContext> options) : DbContext(options)
{
    public DbSet<LibraryConnector> LibraryConnectors { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //LibraryConnector Types
        modelBuilder.Entity<LibraryConnector>()
            .HasDiscriminator(l => l.LibraryType)
            .HasValue<Komga>(LibraryType.Komga)
            .HasValue<Kavita>(LibraryType.Kavita);
    }
}