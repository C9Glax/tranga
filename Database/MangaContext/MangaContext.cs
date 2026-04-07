using Microsoft.EntityFrameworkCore;

namespace Database.MangaContext;

public class MangaContext(DbContextOptions<MangaContext> options) : TrangaDatabaseContext<MangaContext>(options)
{
    public DbSet<DbManga> Mangas { get; init; }
    
    public DbSet<DbGenre> Genres { get; init; }
    
    public DbSet<DbPerson> Authors { get; init; }
    
    public DbSet<DbPerson> Artists { get; init; }
    
    public DbSet<DbChapter> Chapters { get; init; }
    
    public DbSet<DbFile> Files { get; init; }
    
    public DbSet<DbMetadata> MetadataEntries { get; init; }
    
    public DbSet<DbMangaMetadataEntries> MangaMetadataEntries { get; init; }
    
    public DbSet<DbDownloadLink> DownloadLinks { get; init; }
    
    public DbSet<DbMangaDownloadLinks> MangaDownloadLinks { get; init; }
    
    public DbSet<DbChapterDownloadLink> ChapterDownloadLinks { get; init; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        CreateMangaModel(modelBuilder);
        CreateMangaMetadataModel(modelBuilder);
        CreateGenresModel(modelBuilder);
        CreatePersonModel(modelBuilder);
        CreateChapterModel(modelBuilder);
        CreateFileModel(modelBuilder);
        CreateMetadataModel(modelBuilder);
        CreateDownloadLinkModel(modelBuilder);
        CreateMangaDownloadLinkModel(modelBuilder);
        CreateChapterDownloadLinkModel(modelBuilder);
    }

    private void CreateMangaModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbManga>()
            .HasKey(m => m.MangaId);

        modelBuilder.Entity<DbManga>()
            .HasMany<DbChapter>(m => m.Chapters)
            .WithOne(c => c.Manga)
            .HasForeignKey(c => c.MangaId);
    }

    private void CreateMangaMetadataModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbMangaMetadataEntries>()
            .HasKey(m => new { m.MangaId, MetadataSourceId = m.MetadataId });
        
        modelBuilder.Entity<DbManga>()
            .HasMany<DbMangaMetadataEntries>(m => m.MetadataEntries)
            .WithOne(s => s.Manga)
            .HasForeignKey(s => s.MangaId);
        
        modelBuilder.Entity<DbMetadata>()
            .HasMany<DbMangaMetadataEntries>(s => s.MangaMetadataEntries)
            .WithOne(s => s.Metadata)
            .HasForeignKey(s => s.MetadataId);

        modelBuilder.Entity<DbMangaMetadataEntries>()
            .Navigation(s => s.Manga)
            .AutoInclude(true);
        
        modelBuilder.Entity<DbMangaMetadataEntries>()
            .Navigation(s => s.Metadata)
            .AutoInclude(true);
    }

    private void CreateChapterModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbChapter>()
            .HasKey(c => c.ChapterId);
        
        modelBuilder.Entity<DbChapter>()
            .HasMany<DbChapterDownloadLink>(c => c.DownloadLinks)
            .WithOne(s => s.Chapter)
            .HasForeignKey(s => s.ChapterId);
    }

    private void CreateGenresModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbGenre>()
            .HasKey(g => g.Genre);

        modelBuilder.Entity<DbMangaGenres>()
            .HasKey(e => new { MetadataSourceId = e.MetadataId, e.GenreId });
        
        modelBuilder.Entity<DbGenre>()
            .HasMany<DbMetadata>(g => g.MetadataEntries)
            .WithMany(s => s.Genres)
            .UsingEntity<DbMangaGenres>(
                r => r.HasOne<DbMetadata>(e => e.Metadata).WithMany().HasForeignKey(e => e.MetadataId),
                l => l.HasOne<DbGenre>(e => e.Genre).WithMany().HasForeignKey(e => e.GenreId)
            );
    }

    private void CreatePersonModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbPerson>()
            .HasKey(p => p.Name);
        
        
        modelBuilder.Entity<DbPerson>()
            .HasMany<DbMetadata>()
            .WithMany(s => s.Authors)
            .UsingEntity<DbMangaAuthors>(
                r => r.HasOne<DbMetadata>(e => e.Metadata).WithMany().HasForeignKey(e => e.MetadataId),
                l => l.HasOne<DbPerson>(e => e.Author).WithMany().HasForeignKey(e => e.AuthorId)
            );
        
        modelBuilder.Entity<DbPerson>()
            .HasMany<DbMetadata>()
            .WithMany(s => s.Artists)
            .UsingEntity<DbMangaArtists>(
                r => r.HasOne<DbMetadata>(e => e.Metadata).WithMany().HasForeignKey(e => e.MetadataId),
                l => l.HasOne<DbPerson>(e => e.Artist).WithMany().HasForeignKey(e => e.ArtistId)
            );
    }

    private void CreateFileModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbFile>()
            .HasKey(f => f.FileId);

        modelBuilder.Entity<DbFile>()
            .HasIndex(f => new { f.Path, f.Name }, "IX_File_Path_Name");
    }

    private void CreateMetadataModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbMetadata>()
            .HasKey(s => s.MetadataId);

        modelBuilder.Entity<DbMetadata>()
            .HasIndex(s => new { s.MetadataExtension, s.Identifier }, "IX_Metadata_Extension_Identifier");

        modelBuilder.Entity<DbMetadata>()
            .HasOne<DbFile>(m => m.Cover)
            .WithOne()
            .HasForeignKey<DbMetadata>(m => m.CoverId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private void CreateDownloadLinkModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbDownloadLink>()
            .HasKey(s => s.DownloadLinkId);
        
        modelBuilder.Entity<DbDownloadLink>()
            .HasOne<DbFile>(m => m.Cover)
            .WithOne()
            .HasForeignKey<DbDownloadLink>(m => m.CoverId)
            .OnDelete(DeleteBehavior.Cascade);
    }
    
    private void CreateMangaDownloadLinkModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbMangaDownloadLinks>()
            .HasKey(m => new { m.MangaId, DownloadSourceId = m.DownloadLinkId });
        
        modelBuilder.Entity<DbManga>()
            .HasMany<DbMangaDownloadLinks>(m => m.DownloadLinks)
            .WithOne(s => s.Manga)
            .HasForeignKey(s => s.MangaId);
        
        modelBuilder.Entity<DbDownloadLink>()
            .HasMany<DbMangaDownloadLinks>(s => s.MangaMatches)
            .WithOne(s => s.DownloadLink)
            .HasForeignKey(s => s.DownloadLinkId);

        modelBuilder.Entity<DbMangaDownloadLinks>()
            .Navigation(s => s.Manga)
            .AutoInclude(true);
        
        modelBuilder.Entity<DbMangaDownloadLinks>()
            .Navigation(s => s.DownloadLink)
            .AutoInclude(true);
    }

    private void CreateChapterDownloadLinkModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbChapterDownloadLink>()
            .HasKey(s => new { s.ChapterId, s.DownloadExtension });

        modelBuilder.Entity<DbChapterDownloadLink>()
            .HasOne<DbFile>(s => s.File)
            .WithOne()
            .HasForeignKey<DbChapterDownloadLink>(s => s.FileId);
    }
}