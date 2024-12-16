using API.Schema.Jobs;
using API.Schema.LibraryConnectors;
using API.Schema.MangaConnectors;
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
    public DbSet<Notification> Notifications { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MangaConnector>()
            .HasDiscriminator(c => c.Name)
            .HasValue<AsuraToon>("AsuraToon")
            .HasValue<Bato>("Bato")
            .HasValue<MangaHere>("MangaHere")
            .HasValue<MangaKatana>("MangaKatana")
            .HasValue<MangaLife>("Manga4Life")
            .HasValue<Manganato>("Manganato")
            .HasValue<Mangasee>("Mangasee")
            .HasValue<Mangaworld>("Mangaworld")
            .HasValue<ManhuaPlus>("ManhuaPlus")
            .HasValue<Weebcentral>("Weebcentral")
            .HasValue<MangaDex>("MangaDex");
        modelBuilder.Entity<LibraryConnector>()
            .HasDiscriminator<LibraryType>(l => l.LibraryType)
            .HasValue<Komga>(LibraryType.Komga)
            .HasValue<Kavita>(LibraryType.Kavita);
        modelBuilder.Entity<NotificationConnector>()
            .HasDiscriminator<NotificationConnectorType>(n => n.NotificationConnectorType)
            .HasValue<Gotify>(NotificationConnectorType.Gotify)
            .HasValue<Ntfy>(NotificationConnectorType.Ntfy)
            .HasValue<Lunasea>(NotificationConnectorType.LunaSea);
        modelBuilder.Entity<Job>()
            .HasDiscriminator<JobType>(j => j.JobType)
            .HasValue<MoveFileOrFolderJob>(JobType.MoveFileOrFolderJob)
            .HasValue<DownloadNewChaptersJob>(JobType.DownloadNewChaptersJob)
            .HasValue<DownloadSingleChapterJob>(JobType.DownloadSingleChapterJob)
            .HasValue<UpdateMetadataJob>(JobType.UpdateMetaDataJob);

        modelBuilder.Entity<Chapter>()
            .HasOne<Manga>(c => c.ParentManga);
        modelBuilder.Entity<Chapter>()
            .Navigation(c => c.ParentManga)
            .AutoInclude();

        modelBuilder.Entity<Manga>()
            .HasOne<Chapter>(m => m.LatestChapterAvailable)
            .WithOne();
        modelBuilder.Entity<Manga>()
            .HasOne<Chapter>(m => m.LatestChapterDownloaded)
            .WithOne();
        modelBuilder.Entity<Manga>()
            .HasOne<MangaConnector>(m => m.MangaConnector);
        modelBuilder.Entity<Manga>()
            .Navigation(m => m.MangaConnector)
            .AutoInclude();
        modelBuilder.Entity<Manga>()
            .HasMany<Author>(m => m.Authors);
        modelBuilder.Entity<Manga>()
            .Navigation(m => m.Authors)
            .AutoInclude();
        modelBuilder.Entity<Manga>()
            .HasMany<MangaTag>(m => m.Tags);
        modelBuilder.Entity<Manga>()
            .Navigation(m => m.Tags)
            .AutoInclude();
        modelBuilder.Entity<Manga>()
            .HasMany<Link>(m => m.Links);
        modelBuilder.Entity<Manga>()
            .Navigation(m => m.Links)
            .AutoInclude();
        modelBuilder.Entity<Manga>()
            .HasMany<MangaAltTitle>(m => m.AltTitles);
        modelBuilder.Entity<Manga>()
            .Navigation(m => m.AltTitles)
            .AutoInclude();
    }
}