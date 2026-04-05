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
    
    public DbSet<DbMangaMetadataSource> MangaMetadataSources { get; init; }
    
    public DbSet<DbDownloadSource> DownloadSources { get; init; }
    
    public DbSet<DbMangaDownloadSource> MangaDownloadSources { get; init; }
    
    public DbSet<DbChapterDownloadSource> ChapterDownloadSources { get; init; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        CreateMangaModel(modelBuilder);
        CreateMangaMetadataSourceModel(modelBuilder);
        CreateGenresModel(modelBuilder);
        CreatePersonModel(modelBuilder);
        CreateChapterModel(modelBuilder);
        CreateFileModel(modelBuilder);
        CreateMetadataModel(modelBuilder);
        CreateDownloadSourceModel(modelBuilder);
        CreateMangaDownloadSourceModel(modelBuilder);
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
    }

    private void CreateMangaMetadataSourceModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbMangaMetadataSource>()
            .HasKey(m => new { m.MangaId, m.MetadataSourceId });
        
        modelBuilder.Entity<DbManga>()
            .HasMany<DbMangaMetadataSource>(m => m.MetadataSources)
            .WithOne(s => s.Manga)
            .HasForeignKey(s => s.MangaId);
        
        modelBuilder.Entity<DbMetadataSource>()
            .HasMany<DbMangaMetadataSource>(s => s.MangaMetadataSources)
            .WithOne(s => s.MetadataSource)
            .HasForeignKey(s => s.MetadataSourceId);

        modelBuilder.Entity<DbMangaMetadataSource>()
            .Navigation(s => s.Manga)
            .AutoInclude(true);
        
        modelBuilder.Entity<DbMangaMetadataSource>()
            .Navigation(s => s.MetadataSource)
            .AutoInclude(true);
    }

    private void CreateChapterModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbChapter>()
            .HasKey(c => c.ChapterId);
        
        modelBuilder.Entity<DbChapter>()
            .HasMany<DbChapterDownloadSource>(c => c.DownloadSources)
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
            .HasForeignKey<DbMetadataSource>(m => m.CoverId);
    }

    private void CreateDownloadSourceModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbDownloadSource>()
            .HasKey(s => s.DownloadId);
        
        modelBuilder.Entity<DbDownloadSource>()
            .HasOne<DbFile>(m => m.Cover)
            .WithOne()
            .HasForeignKey<DbDownloadSource>(m => m.CoverId);
    }
    
    private void CreateMangaDownloadSourceModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbMangaDownloadSource>()
            .HasKey(m => new { m.MangaId, m.DownloadSourceId });
        
        modelBuilder.Entity<DbManga>()
            .HasMany<DbMangaDownloadSource>(m => m.DownloadSources)
            .WithOne(s => s.Manga)
            .HasForeignKey(s => s.MangaId);
        
        modelBuilder.Entity<DbDownloadSource>()
            .HasMany<DbMangaDownloadSource>(s => s.MangaDownloadSources)
            .WithOne(s => s.DownloadSource)
            .HasForeignKey(s => s.DownloadSourceId);

        modelBuilder.Entity<DbMangaDownloadSource>()
            .Navigation(s => s.Manga)
            .AutoInclude(true);
        
        modelBuilder.Entity<DbMangaDownloadSource>()
            .Navigation(s => s.DownloadSource)
            .AutoInclude(true);
    }

    private void CreateChapterDownloadModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbChapterDownloadSource>()
            .HasKey(s => new { s.ChapterId, s.DownloadExtension });

        modelBuilder.Entity<DbChapterDownloadSource>()
            .HasOne<DbFile>(s => s.File)
            .WithOne()
            .HasForeignKey<DbChapterDownloadSource>(s => s.FileId);
    }
}