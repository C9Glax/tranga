using API.Schema.MangaContext.MangaConnectors;
using API.Schema.MangaContext.MetadataFetchers;
using Microsoft.EntityFrameworkCore;

namespace API.Schema.MangaContext;

public class MangaContext(DbContextOptions<MangaContext> options) : TrangaBaseContext<MangaContext>(options)
{
    public DbSet<MangaConnector> MangaConnectors { get; set; }
    public DbSet<Manga> Mangas { get; set; }
    public DbSet<FileLibrary> LocalLibraries { get; set; }
    public DbSet<Chapter> Chapters { get; set; }
    public DbSet<Author> Authors { get; set; }
    public DbSet<MangaTag> Tags { get; set; }
    public DbSet<MangaConnectorId<Manga>> MangaConnectorToManga { get; set; }
    public DbSet<MangaConnectorId<Chapter>> MangaConnectorToChapter { get; set; }
    public DbSet<MetadataEntry> MetadataEntries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
        
        modelBuilder.Entity<MetadataFetcher>()
            .HasDiscriminator<string>(nameof(MetadataEntry))
            .HasValue<MyAnimeList>(nameof(MyAnimeList));
        //MetadataEntry
        modelBuilder.Entity<MetadataEntry>()
            .HasOne<Manga>(entry => entry.Manga)
            .WithMany()
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<MetadataEntry>()
            .HasOne<MetadataFetcher>(entry => entry.MetadataFetcher)
            .WithMany()
            .OnDelete(DeleteBehavior.Cascade);
    }
}