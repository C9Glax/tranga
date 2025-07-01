using API.Schema.LibraryConnectors;
using log4net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace API.Schema.Contexts;

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