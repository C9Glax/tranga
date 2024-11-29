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
            .HasValue<CreateArchiveJob>(JobType.CreateArchiveJob)
            .HasValue<MoveFileOrFolderJob>(JobType.MoveFileOrFolderJob)
            .HasValue<ProcessImagesJob>(JobType.ProcessImagesJob)
            .HasValue<DownloadNewChaptersJob>(JobType.DownloadNewChaptersJob)
            .HasValue<DownloadSingleChapterJob>(JobType.DownloadSingleChapterJob)
            .HasValue<UpdateMetadataJob>(JobType.UpdateMetaDataJob)
            .HasValue<CreateComicInfoXmlJob>(JobType.CreateComicInfoXmlJob);

        modelBuilder.Entity<Chapter>()
            .HasOne<Manga>(c => c.ParentManga)
            .WithMany(m => m.Chapters)
            .HasForeignKey(c => c.ParentMangaId);

        modelBuilder.Entity<Manga>()
            .HasOne<Chapter>(m => m.LatestChapterAvailable)
            .WithOne();
        modelBuilder.Entity<Manga>()
            .HasOne<Chapter>(m => m.LatestChapterDownloaded)
            .WithOne();
        modelBuilder.Entity<Manga>()
            .HasOne<MangaConnector>(m => m.MangaConnector)
            .WithMany(c => c.Mangas)
            .HasForeignKey(m => m.MangaConnectorId);
        modelBuilder.Entity<Manga>()
            .HasMany<Author>(m => m.Authors)
            .WithMany(a => a.Mangas)
            .UsingEntity(
                "MangaAuthor",
                l => l.HasOne(typeof(Manga)).WithMany().HasForeignKey("MangaId").HasPrincipalKey("MangaId"),
                r => r.HasOne(typeof(Author)).WithMany().HasForeignKey("AuthorId").HasPrincipalKey("AuthorId"),
                j => j.HasKey("MangaId", "AuthorId"));
        modelBuilder.Entity<Manga>()
            .HasMany<MangaTag>(m => m.Tags)
            .WithMany(t => t.Mangas)
            .UsingEntity(
                "MangaTag",
                l => l.HasOne(typeof(Manga)).WithMany().HasForeignKey("MangaId").HasPrincipalKey("MangaId"),
                r => r.HasOne(typeof(MangaTag)).WithMany().HasForeignKey("Tag").HasPrincipalKey("Tag"),
                j => j.HasKey("MangaId", "Tag"));
        modelBuilder.Entity<Manga>()
            .HasMany<Link>(m => m.Links)
            .WithOne(c => c.Manga);
        modelBuilder.Entity<Manga>()
            .HasMany<MangaAltTitle>(m => m.AltTitles)
            .WithOne(c => c.Manga);
    }
}