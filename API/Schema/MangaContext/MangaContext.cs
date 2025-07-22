using API.MangaConnectors;
using API.Schema.MangaContext.MetadataFetchers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace API.Schema.MangaContext;

public class MangaContext(DbContextOptions<MangaContext> options) : TrangaBaseContext<MangaContext>(options)
{
    public DbSet<Manga> Mangas { get; set; }
    public DbSet<FileLibrary> FileLibraries { get; set; }
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
        //Chapter has MangaConnectorIds
        modelBuilder.Entity<Chapter>()
            .HasMany<MangaConnectorId<Chapter>>(c => c.MangaConnectorIds)
            .WithOne(id => id.Obj)
            .HasForeignKey(id => id.ObjId)
            .OnDelete(DeleteBehavior.Cascade);
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
        
        
        //FileLibrary has many Mangas
        modelBuilder.Entity<FileLibrary>()
            .HasMany<Manga>()
            .WithOne(m => m.Library)
            .HasForeignKey(m => m.LibraryId)
            .OnDelete(DeleteBehavior.SetNull);
        
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

    public Manga? FindMangaLike(Manga other)
    {
        if (MangaIncludeAll().FirstOrDefault(m => m.Key == other.Key) is { } f)
            return f;

        return MangaIncludeAll()
            .FirstOrDefault(m => m.Links.Any(l => l.Key == other.Key) ||
                                 m.AltTitles.Any(t => other.AltTitles.Select(ot => ot.Title)
                                     .Any(s => s.Equals(t.Title))));
    }

    public IIncludableQueryable<Manga, ICollection<MangaConnectorId<Manga>>> MangaIncludeAll() => Mangas.Include(m => m.Library)
        .Include(m => m.Authors)
        .Include(m => m.MangaTags)
        .Include(m => m.Links)
        .Include(m => m.AltTitles)
        .Include(m => m.Chapters)
        .Include(m => m.MangaConnectorIds);
}