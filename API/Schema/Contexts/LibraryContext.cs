using API.Schema.LibraryConnectors;
using log4net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace API.Schema.Contexts;

public class LibraryContext(DbContextOptions<LibraryContext> options) : DbContext(options)
{
    public DbSet<LibraryConnector> LibraryConnectors { get; set; }
    
    private ILog Log => LogManager.GetLogger(GetType());

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTrackingWithIdentityResolution);
        optionsBuilder.LogTo(s =>
        {
            Log.Debug(s);
        }, [DbLoggerCategory.Query.Name], LogLevel.Trace, DbContextLoggerOptions.Level | DbContextLoggerOptions.Category);
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //LibraryConnector Types
        modelBuilder.Entity<LibraryConnector>()
            .HasDiscriminator(l => l.LibraryType)
            .HasValue<Komga>(LibraryType.Komga)
            .HasValue<Kavita>(LibraryType.Kavita);
    }
}