using API.Schema.Jobs;
using API.Schema.MangaConnectors;
using log4net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace API.Schema.Contexts;

public class PgsqlContext(DbContextOptions<PgsqlContext> options) : DbContext(options)
{
    public DbSet<Job> Jobs { get; set; }
    public DbSet<MangaConnector> MangaConnectors { get; set; }
    public DbSet<Manga> Mangas { get; set; }
    public DbSet<LocalLibrary> LocalLibraries { get; set; }
    public DbSet<Chapter> Chapters { get; set; }
    public DbSet<Author> Authors { get; set; }
    public DbSet<MangaTag> Tags { get; set; }
    private ILog Log => LogManager.GetLogger(GetType());
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        optionsBuilder.LogTo(s =>
        {
            Log.Debug(s);
        }, [DbLoggerCategory.Query.Name], LogLevel.Trace, DbContextLoggerOptions.Level | DbContextLoggerOptions.Category);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        //Job Types
        modelBuilder.Entity<Job>()
            .HasDiscriminator(j => j.JobType)
            .HasValue<MoveFileOrFolderJob>(JobType.MoveFileOrFolderJob)
            .HasValue<MoveMangaLibraryJob>(JobType.MoveMangaLibraryJob)
            .HasValue<DownloadAvailableChaptersJob>(JobType.DownloadAvailableChaptersJob)
            .HasValue<DownloadSingleChapterJob>(JobType.DownloadSingleChapterJob)
            .HasValue<DownloadMangaCoverJob>(JobType.DownloadMangaCoverJob)
            .HasValue<RetrieveChaptersJob>(JobType.RetrieveChaptersJob)
            .HasValue<UpdateChaptersDownloadedJob>(JobType.UpdateChaptersDownloadedJob)
            .HasValue<UpdateSingleChapterDownloadedJob>(JobType.UpdateSingleChapterDownloadedJob);
        
        //Job specification
        modelBuilder.Entity<DownloadAvailableChaptersJob>()
            .HasOne<Manga>(j => j.Manga)
            .WithMany()
            .HasForeignKey(j => j.MangaId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<DownloadAvailableChaptersJob>()
            .Navigation(j => j.Manga)
            .EnableLazyLoading();
        modelBuilder.Entity<DownloadMangaCoverJob>()
            .HasOne<Manga>(j => j.Manga)
            .WithMany()
            .HasForeignKey(j => j.MangaId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<DownloadMangaCoverJob>()
            .Navigation(j => j.Manga)
            .EnableLazyLoading();
        modelBuilder.Entity<DownloadSingleChapterJob>()
            .HasOne<Chapter>(j => j.Chapter)
            .WithMany()
            .HasForeignKey(j => j.ChapterId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<DownloadSingleChapterJob>()
            .Navigation(j => j.Chapter)
            .EnableLazyLoading();
        modelBuilder.Entity<MoveMangaLibraryJob>()
            .HasOne<Manga>(j => j.Manga)
            .WithMany()
            .HasForeignKey(j => j.MangaId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MoveMangaLibraryJob>()
            .Navigation(j => j.Manga)
            .EnableLazyLoading();
        modelBuilder.Entity<MoveMangaLibraryJob>()
            .HasOne<LocalLibrary>(j => j.ToLibrary)
            .WithMany()
            .HasForeignKey(j => j.ToLibraryId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MoveMangaLibraryJob>()
            .Navigation(j => j.ToLibrary)
            .EnableLazyLoading();
        modelBuilder.Entity<RetrieveChaptersJob>()
            .HasOne<Manga>(j => j.Manga)
            .WithMany()
            .HasForeignKey(j => j.MangaId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<RetrieveChaptersJob>()
            .Navigation(j => j.Manga)
            .EnableLazyLoading();
        modelBuilder.Entity<UpdateChaptersDownloadedJob>()
            .HasOne<Manga>(j => j.Manga)
            .WithMany()
            .HasForeignKey(j => j.MangaId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<UpdateChaptersDownloadedJob>()
            .Navigation(j => j.Manga)
            .EnableLazyLoading();
        
        //Job has possible ParentJob
        modelBuilder.Entity<Job>()
            .HasOne<Job>(childJob => childJob.ParentJob)
            .WithMany()
            .HasForeignKey(childjob => childjob.ParentJobId)
            .OnDelete(DeleteBehavior.Cascade);
        //Job might be dependent on other Jobs
        modelBuilder.Entity<Job>()
            .HasMany<Job>(root => root.DependsOnJobs)
            .WithMany();
        modelBuilder.Entity<Job>()
            .Navigation(j => j.DependsOnJobs)
            .AutoInclude(false)
            .EnableLazyLoading();
        
        //MangaConnector Types
        modelBuilder.Entity<MangaConnector>()
            .HasDiscriminator(c => c.Name)
            .HasValue<Global>("Global")
            .HasValue<MangaDex>("MangaDex")
            .HasValue<ComickIo>("ComickIo");
        //MangaConnector is responsible for many Manga
        modelBuilder.Entity<MangaConnector>()
            .HasMany<Manga>()
            .WithOne(m => m.MangaConnector)
            .HasForeignKey(m => m.MangaConnectorName)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Manga>()
            .Navigation(m => m.MangaConnector)
            .AutoInclude();

        //Manga has many Chapters
        modelBuilder.Entity<Manga>()
            .HasMany<Chapter>(m => m.Chapters)
            .WithOne(c => c.ParentManga)
            .HasForeignKey(c => c.ParentMangaId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Chapter>()
            .Navigation(c => c.ParentManga)
            .AutoInclude();
        modelBuilder.Entity<Manga>()
            .Navigation(m => m.Chapters)
            .AutoInclude(false)
            .EnableLazyLoading();
        //Manga owns MangaAltTitles
        modelBuilder.Entity<Manga>()
            .OwnsMany<MangaAltTitle>(m => m.AltTitles)
            .WithOwner();
        modelBuilder.Entity<Manga>()
            .Navigation(m => m.AltTitles)
            .AutoInclude();
        //Manga owns Links
        modelBuilder.Entity<Manga>()
            .OwnsMany<Link>(m => m.Links)
            .WithOwner();
        modelBuilder.Entity<Manga>()
            .Navigation(m => m.Links)
            .AutoInclude();
        //Manga has many Tags associated with many Manga
        modelBuilder.Entity<Manga>()
            .HasMany<MangaTag>(m => m.MangaTags)
            .WithMany()
            .UsingEntity("MangaTagToManga",
                l=> l.HasOne(typeof(MangaTag)).WithMany().HasForeignKey("MangaTagIds").HasPrincipalKey(nameof(MangaTag.Tag)),
                r => r.HasOne(typeof(Manga)).WithMany().HasForeignKey("MangaIds").HasPrincipalKey(nameof(Manga.MangaId)),
                j => j.HasKey("MangaTagIds", "MangaIds")
            );
        modelBuilder.Entity<Manga>()
            .Navigation(m => m.MangaTags)
            .AutoInclude();
        //Manga has many Authors associated with many Manga
        modelBuilder.Entity<Manga>()
            .HasMany<Author>(m => m.Authors)
            .WithMany()
            .UsingEntity("AuthorToManga",
                l=> l.HasOne(typeof(Author)).WithMany().HasForeignKey("AuthorIds").HasPrincipalKey(nameof(Author.AuthorId)),
                r => r.HasOne(typeof(Manga)).WithMany().HasForeignKey("MangaIds").HasPrincipalKey(nameof(Manga.MangaId)),
                j => j.HasKey("AuthorIds", "MangaIds")
            );
        modelBuilder.Entity<Manga>()
            .Navigation(m => m.Authors)
            .AutoInclude();
        
        //LocalLibrary has many Mangas
        modelBuilder.Entity<LocalLibrary>()
            .HasMany<Manga>()
            .WithOne(m => m.Library)
            .HasForeignKey(m => m.LibraryId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Manga>()
            .Navigation(m => m.Library)
            .AutoInclude();
    }
}