using API.Schema.LibraryContext.LibraryConnectors;
using Microsoft.EntityFrameworkCore;

namespace API.Schema.LibraryContext;

public class LibraryContext(DbContextOptions<LibraryContext> options) : TrangaBaseContext<LibraryContext>(options)
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