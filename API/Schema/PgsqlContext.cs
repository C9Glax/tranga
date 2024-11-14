using API.Schema.Jobs;
using API.Schema.LibraryConnectors;
using API.Schema.NotificationConnectors;
using Microsoft.EntityFrameworkCore;

namespace API.Schema;

public class PgsqlContext(DbContextOptions<PgsqlContext> options) : DbContext(options)
{
    public DbSet<Job> Jobs { get; set; }
    public DbSet<MangaConnector> MangaConnectors { get; set; }
    public DbSet<Manga> Manga { get; set; }
    public DbSet<Chapter> Chapters { get; set; }
    public DbSet<Author> Authors { get; set; }
    public DbSet<Link> Link { get; set; }
    public DbSet<MangaTag> Tags { get; set; }
    public DbSet<MangaAltTitle> AltTitles { get; set; }
    public DbSet<LibraryConnector> LibraryConnectors { get; set; }
    public DbSet<NotificationConnector> NotificationConnectors { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Job>()
            .HasOne<Job>("ParentJobId");
        modelBuilder.Entity<Job>()
            .HasOne<Job>("DependsOnJobId");
        modelBuilder.Entity<DownloadNewChaptersJob>()
            .HasOne<Manga>("MangaId");
        modelBuilder.Entity<DownloadSingleChapterJob>()
            .HasOne<Chapter>("ChapterId");
        modelBuilder.Entity<UpdateMetadataJob>()
            .HasOne<Manga>("MangaId");
        modelBuilder.Entity<Chapter>()
            .HasOne<Manga>("ParentMangaId");
        modelBuilder.Entity<Manga>()
            .HasOne<MangaConnector>("MangaConnectorId");
        modelBuilder.Entity<Manga>()
            .HasMany<Author>("AuthorIds");
        modelBuilder.Entity<Manga>()
            .HasMany<MangaTag>("TagIds");
        modelBuilder.Entity<Manga>()
            .HasMany<Link>("LinkIds");
        modelBuilder.Entity<Manga>()
            .HasMany<MangaAltTitle>("AltTitleIds");
    }
}