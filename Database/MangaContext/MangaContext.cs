using Microsoft.EntityFrameworkCore;

namespace Database.MangaContext;

public class MangaContext(DbContextOptions<MangaContext> options) : TrangaDatabaseContext<MangaContext>(options)
{
    public DbSet<DbManga> Mangas { get; init; }
    
    public DbSet<DbGenre> Genres { get; init; }
    
    public DbSet<DbPerson> MangaAuthors { get; init; }
    
    public DbSet<DbPerson> MangaArtists { get; init; }
    
    public DbSet<DbChapter> Chapters { get; init; }
    
    public DbSet<DbFile> Files { get; init; }
    
    public DbSet<DbMetadataSource> MetadataSources { get; init; }
    
    public DbSet<DbMangaDownloadSources> MangaDownloadSources { get; init; }
    
    public DbSet<DbChapterDownloadSources> ChapterDownloadSources { get; init; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        CreateMangaModel(modelBuilder);
        CreateGenresModel(modelBuilder);
        CreatePersonModel(modelBuilder);
        CreateChapterModel(modelBuilder);
        CreateFileModel(modelBuilder);
        CreateMetadataModel(modelBuilder);
        CreateMangaDownloadModel(modelBuilder);
        CreateChapterDownloadModel(modelBuilder);
    }

    private void CreateMangaModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbManga>()
            .HasKey(m => m.MangaId);

        modelBuilder.Entity<DbManga>()
            .HasMany<DbChapter>(m => m.Chapters)
            .WithOne(c => c.Manga)
            .HasForeignKey(c => c.MangaId);
        
        modelBuilder.Entity<DbManga>()
            .HasMany<DbMetadataSource>(m => m.MetadataSources)
            .WithOne(s => s.Manga)
            .HasForeignKey(s => s.MangaId);
        
        modelBuilder.Entity<DbManga>()
            .HasMany<DbMangaDownloadSources>(m => m.DownloadSources)
            .WithOne(s => s.Manga)
            .HasForeignKey(s => s.MangaId);
    }

    private void CreateChapterModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbChapter>()
            .HasKey(c => c.ChapterId);
        
        modelBuilder.Entity<DbChapter>()
            .HasMany<DbChapterDownloadSources>(c => c.DownloadSources)
            .WithOne(s => s.Chapter)
            .HasForeignKey(s => s.ChapterId);
    }

    private void CreateGenresModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbGenre>()
            .HasKey(g => g.Genre);

        modelBuilder.Entity<DbMangaGenres>()
            .HasKey(e => new { e.MetadataSourceId, e.GenreId });
        
        modelBuilder.Entity<DbGenre>()
            .HasMany<DbMetadataSource>(g => g.MetadataSources)
            .WithMany(s => s.Genres)
            .UsingEntity<DbMangaGenres>(
                r => r.HasOne<DbMetadataSource>(e => e.MetadataSource).WithMany().HasForeignKey(e => e.MetadataSourceId),
                l => l.HasOne<DbGenre>(e => e.Genre).WithMany().HasForeignKey(e => e.GenreId)
            );
    }

    private void CreatePersonModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbPerson>()
            .HasKey(p => p.Name);
        
        
        modelBuilder.Entity<DbPerson>()
            .HasMany<DbMetadataSource>()
            .WithMany(s => s.Authors)
            .UsingEntity<DbMangaAuthors>(
                r => r.HasOne<DbMetadataSource>(e => e.MetadataSource).WithMany().HasForeignKey(e => e.MetadataSourceId),
                l => l.HasOne<DbPerson>(e => e.Author).WithMany().HasForeignKey(e => e.AuthorId)
            );
        
        modelBuilder.Entity<DbPerson>()
            .HasMany<DbMetadataSource>()
            .WithMany(s => s.Artists)
            .UsingEntity<DbMangaArtists>(
                r => r.HasOne<DbMetadataSource>(e => e.MetadataSource).WithMany().HasForeignKey(e => e.MetadataSourceId),
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
        modelBuilder.Entity<DbMetadataSource>()
            .HasKey(s => s.MetadataId);

        modelBuilder.Entity<DbMetadataSource>()
            .HasIndex(s => new { s.MetadataExtension, s.Identifier }, "IX_Metadata_Extension_Identifier");

        modelBuilder.Entity<DbMetadataSource>()
            .HasOne<DbFile>(m => m.Cover)
            .WithOne()
            .HasPrincipalKey<DbMetadataSource>(m => m.CoverId);
    }

    private void CreateMangaDownloadModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbMangaDownloadSources>()
            .HasKey(s => new { s.MangaId, s.DownloadExtension });
    }

    private void CreateChapterDownloadModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbChapterDownloadSources>()
            .HasKey(s => new { s.ChapterId, s.DownloadExtension });

        modelBuilder.Entity<DbChapterDownloadSources>()
            .HasOne<DbFile>(s => s.File)
            .WithOne()
            .HasForeignKey<DbChapterDownloadSources>(s => s.FileId);
    }
}