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
    public DbSet<Manga> Mangas { get; set; }
    public DbSet<LocalLibrary> LocalLibraries { get; set; }
    public DbSet<Chapter> Chapters { get; set; }
    public DbSet<Author> Authors { get; set; }
    public DbSet<Link> Links { get; set; }
    public DbSet<MangaTag> Tags { get; set; }
    public DbSet<MangaAltTitle> AltTitles { get; set; }
    public DbSet<LibraryConnector> LibraryConnectors { get; set; }
    public DbSet<NotificationConnector> NotificationConnectors { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MangaConnector>()
            .HasDiscriminator(c => c.Name)
            .HasValue<Global>("Global")
            .HasValue<AsuraToon>("AsuraToon")
            .HasValue<Bato>("Bato")
            .HasValue<MangaHere>("MangaHere")
            .HasValue<MangaKatana>("MangaKatana")
            .HasValue<Mangaworld>("Mangaworld")
            .HasValue<ManhuaPlus>("ManhuaPlus")
            .HasValue<Weebcentral>("Weebcentral")
            .HasValue<Manganato>("Manganato")
            .HasValue<MangaDex>("MangaDex");
        modelBuilder.Entity<LibraryConnector>()
            .HasDiscriminator<LibraryType>(l => l.LibraryType)
            .HasValue<Komga>(LibraryType.Komga)
            .HasValue<Kavita>(LibraryType.Kavita);

        modelBuilder.Entity<Job>()
            .HasDiscriminator<JobType>(j => j.JobType)
            .HasValue<MoveFileOrFolderJob>(JobType.MoveFileOrFolderJob)
            .HasValue<DownloadAvailableChaptersJob>(JobType.DownloadAvailableChaptersJob)
            .HasValue<DownloadSingleChapterJob>(JobType.DownloadSingleChapterJob)
            .HasValue<DownloadMangaCoverJob>(JobType.DownloadMangaCoverJob)
            .HasValue<UpdateMetadataJob>(JobType.UpdateMetaDataJob)
            .HasValue<RetrieveChaptersJob>(JobType.RetrieveChaptersJob)
            .HasValue<UpdateFilesDownloadedJob>(JobType.UpdateFilesDownloadedJob);
        modelBuilder.Entity<Job>()
            .HasMany<Job>()
            .WithOne(j => j.ParentJob)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Job>()
            .HasMany<Job>(j => j.DependsOnJobs)
            .WithMany();
        modelBuilder.Entity<DownloadAvailableChaptersJob>()
            .Navigation(dncj => dncj.Manga)
            .AutoInclude();
        modelBuilder.Entity<DownloadSingleChapterJob>()
            .Navigation(dscj => dscj.Chapter)
            .AutoInclude();
        modelBuilder.Entity<UpdateMetadataJob>()
            .Navigation(umj => umj.Manga)
            .AutoInclude();

        modelBuilder.Entity<Manga>()
            .HasOne<MangaConnector>(m => m.MangaConnector)
            .WithMany()
            .HasForeignKey(m => m.MangaConnectorId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Manga>()
            .Navigation(m => m.MangaConnector)
            .AutoInclude();
        modelBuilder.Entity<Manga>()
            .HasOne<LocalLibrary>(m => m.Library)
            .WithMany()
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Manga>()
            .Navigation(m => m.Library)
            .AutoInclude();
        modelBuilder.Entity<Manga>()
            .HasMany<Author>(m => m.Authors)
            .WithMany();
        modelBuilder.Entity<Manga>()
            .Navigation(m => m.Authors)
            .AutoInclude();
        modelBuilder.Entity<Manga>()
            .HasMany<MangaTag>(m => m.MangaTags)
            .WithMany();
        modelBuilder.Entity<Manga>()
            .Navigation(m => m.MangaTags)
            .AutoInclude();
        modelBuilder.Entity<Manga>()
            .HasMany<Link>(m => m.Links)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Manga>()
            .Navigation(m => m.Links)
            .AutoInclude();
        modelBuilder.Entity<Manga>()
            .HasMany<MangaAltTitle>(m => m.AltTitles)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Manga>()
            .Navigation(m => m.AltTitles)
            .AutoInclude();
        modelBuilder.Entity<Chapter>()
            .HasOne<Manga>(c => c.ParentManga)
            .WithMany()
            .HasForeignKey(c => c.ParentMangaId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Chapter>()
            .Navigation(c => c.ParentManga)
            .AutoInclude();
    }
}