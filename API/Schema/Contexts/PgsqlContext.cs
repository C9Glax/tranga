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
    public DbSet<FileLibrary> LocalLibraries { get; set; }
    public DbSet<Chapter> Chapters { get; set; }
    public DbSet<Author> Authors { get; set; }
    public DbSet<MangaTag> Tags { get; set; }
    public DbSet<MangaConnectorId<Manga>> MangaConnectorToManga { get; set; }
    public DbSet<MangaConnectorId<Chapter>> MangaConnectorToChapter { get; set; }
    private ILog Log => LogManager.GetLogger(GetType());
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.EnableSensitiveDataLogging();
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
            .HasValue<UpdateCoverJob>(JobType.UpdateCoverJob)
            .HasValue<UpdateChaptersDownloadedJob>(JobType.UpdateChaptersDownloadedJob);

        modelBuilder.Entity<DownloadAvailableChaptersJob>()
            .HasOne<Manga>(j => j.Manga)
            .WithMany()
            .HasForeignKey(j => j.MangaId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<DownloadAvailableChaptersJob>()
            .Navigation(j => j.Manga)
            .EnableLazyLoading();
        modelBuilder.Entity<DownloadMangaCoverJob>()
            .HasOne<Manga>(j => j.Manga)
            .WithMany()
            .HasForeignKey(j => j.MangaId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<DownloadMangaCoverJob>()
            .Navigation(j => j.Manga)
            .EnableLazyLoading();
        modelBuilder.Entity<DownloadSingleChapterJob>()
            .HasOne<Chapter>(j => j.Chapter)
            .WithMany()
            .HasForeignKey(j => j.ChapterId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<DownloadSingleChapterJob>()
            .Navigation(j => j.Chapter)
            .EnableLazyLoading();
        modelBuilder.Entity<MoveMangaLibraryJob>()
            .HasOne<Manga>(j => j.Manga)
            .WithMany()
            .HasForeignKey(j => j.MangaId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MoveMangaLibraryJob>()
            .Navigation(j => j.Manga)
            .EnableLazyLoading();
        modelBuilder.Entity<MoveMangaLibraryJob>()
            .HasOne<FileLibrary>(j => j.ToFileLibrary)
            .WithMany()
            .HasForeignKey(j => j.ToLibraryId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MoveMangaLibraryJob>()
            .Navigation(j => j.ToFileLibrary)
            .EnableLazyLoading();
        modelBuilder.Entity<RetrieveChaptersJob>()
            .HasOne<Manga>(j => j.Manga)
            .WithMany()
            .HasForeignKey(j => j.MangaId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<RetrieveChaptersJob>()
            .Navigation(j => j.Manga)
            .EnableLazyLoading();
        modelBuilder.Entity<UpdateChaptersDownloadedJob>()
            .HasOne<Manga>(j => j.Manga)
            .WithMany()
            .HasForeignKey(j => j.MangaId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<UpdateChaptersDownloadedJob>()
            .Navigation(j => j.Manga)
            .EnableLazyLoading();
        
        //Job has possible ParentJob
        modelBuilder.Entity<Job>()
            .HasOne<Job>(childJob => childJob.ParentJob)
            .WithMany()
            .HasForeignKey(childJob => childJob.ParentJobId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Job>()
            .Navigation(childJob => childJob.ParentJob)
            .EnableLazyLoading();
        //Job might be dependent on other Jobs
        modelBuilder.Entity<Job>()
            .HasMany<Job>(root => root.DependsOnJobs)
            .WithMany();
        modelBuilder.Entity<Job>()
            .Navigation(j => j.DependsOnJobs)
            .EnableLazyLoading();
        
        //MangaConnector Types
        modelBuilder.Entity<MangaConnector>()
            .HasDiscriminator(c => c.Name)
            .HasValue<Global>("Global")
            .HasValue<MangaDex>("MangaDex")
            .HasValue<ComickIo>("ComickIo");

        //Manga has many Chapters
        modelBuilder.Entity<Manga>()
            .HasMany<Chapter>(m => m.Chapters)
            .WithOne(c => c.ParentManga)
            .HasForeignKey(c => c.ParentMangaId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Manga>()
            .Navigation(m => m.Chapters)
            .EnableLazyLoading();
        modelBuilder.Entity<Chapter>()
            .Navigation(c => c.ParentManga)
            .EnableLazyLoading();
        //Chapter has MangaConnectorIds
        modelBuilder.Entity<Chapter>()
            .HasMany<MangaConnectorId<Chapter>>(c => c.MangaConnectorIds)
            .WithOne(id => id.Obj)
            .HasForeignKey(id => id.ObjId)
            .OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<MangaConnectorId<Chapter>>()
            .HasOne<MangaConnector>(id => id.MangaConnector)
            .WithMany()
            .HasForeignKey(id => id.MangaConnectorName)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MangaConnectorId<Chapter>>()
            .Navigation(entry => entry.MangaConnector)
            .EnableLazyLoading();
        //Manga owns MangaAltTitles
        modelBuilder.Entity<Manga>()
            .OwnsMany<AltTitle>(m => m.AltTitles)
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
        //Manga has many Tags associated with many Obj
        modelBuilder.Entity<Manga>()
            .HasMany<MangaTag>(m => m.MangaTags)
            .WithMany()
            .UsingEntity("MangaTagToManga",
                l=> l.HasOne(typeof(MangaTag)).WithMany().HasForeignKey("MangaTagIds").HasPrincipalKey(nameof(MangaTag.Tag)),
                r => r.HasOne(typeof(Manga)).WithMany().HasForeignKey("MangaIds").HasPrincipalKey(nameof(Manga.Key)),
                j => j.HasKey("MangaTagIds", "MangaIds")
            );
        modelBuilder.Entity<Manga>()
            .Navigation(m => m.MangaTags)
            .AutoInclude();
        //Manga has many Authors associated with many Obj
        modelBuilder.Entity<Manga>()
            .HasMany<Author>(m => m.Authors)
            .WithMany()
            .UsingEntity("AuthorToManga",
                l=> l.HasOne(typeof(Author)).WithMany().HasForeignKey("AuthorIds").HasPrincipalKey(nameof(Author.Key)),
                r => r.HasOne(typeof(Manga)).WithMany().HasForeignKey("MangaIds").HasPrincipalKey(nameof(Manga.Key)),
                j => j.HasKey("AuthorIds", "MangaIds")
            );
        modelBuilder.Entity<Manga>()
            .Navigation(m => m.Authors)
            .AutoInclude();
        //Manga has many MangaIds
        modelBuilder.Entity<Manga>()
            .HasMany<MangaConnectorId<Manga>>(m => m.MangaConnectorIds)
            .WithOne(id => id.Obj)
            .HasForeignKey(id => id.ObjId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<Manga>()
            .Navigation(m => m.MangaConnectorIds)
            .EnableLazyLoading();
        modelBuilder.Entity<MangaConnectorId<Manga>>()
            .HasOne<MangaConnector>(id => id.MangaConnector)
            .WithMany()
            .HasForeignKey(id => id.MangaConnectorName)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MangaConnectorId<Manga>>()
            .Navigation(entry => entry.MangaConnector)
            .EnableLazyLoading();
        
        
        //FileLibrary has many Mangas
        modelBuilder.Entity<FileLibrary>()
            .HasMany<Manga>()
            .WithOne(m => m.Library)
            .HasForeignKey(m => m.LibraryId)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Manga>()
            .Navigation(m => m.Library)
            .EnableLazyLoading();
    }
}