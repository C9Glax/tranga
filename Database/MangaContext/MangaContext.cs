using Microsoft.EntityFrameworkCore;

namespace Database.MangaContext;

public class MangaContext(DbContextOptions<MangaContext> options) : TrangaDatabaseContext<MangaContext>(options)
{
    public DbSet<DbManga> Mangas { get; init; }
    
    public DbSet<DbDownloadLink> DownloadLinks { get; init; }
    
    public DbSet<DbMetadataLink> MetadataLinks { get; init; }
    
    public DbSet<DbChapter> Chapters { get; init; }
    
    public DbSet<DbFile> Files { get; init; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        #region DbManga

        modelBuilder.Entity<DbManga>()
            .HasKey(m => m.Id);

        modelBuilder.Entity<DbManga>()
            .HasMany(m => m.DownloadLinks)
            .WithOne(d => d.Manga)
            .HasForeignKey(d => d.MangaId)
            .OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<DbManga>()
            .HasMany(m => m.MetadataLinks)
            .WithOne(l => l.Manga)
            .HasForeignKey(l => l.MangaId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<DbManga>()
            .Property(m => m.Series)
            .HasMaxLength(1024);

        #endregion

        #region DbChapter

        modelBuilder.Entity<DbChapter>()
            .HasKey(c => c.Id);

        modelBuilder.Entity<DbChapter>()
            .HasOne(c => c.File)
            .WithOne()
            .HasForeignKey<DbChapter>(c => c.FileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DbChapter>()
            .HasIndex(c => new { c.DownloadExtensionId, c.Identifier });
        
        #endregion
        
        #region DbDownloadLink

        modelBuilder.Entity<DbDownloadLink>()
            .HasKey(d => d.Id);

        modelBuilder.Entity<DbDownloadLink>()
            .HasOne(l => l.Cover)
            .WithOne()
            .HasForeignKey<DbDownloadLink>(d => d.CoverId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DbDownloadLink>()
            .HasIndex(d => new { d.MangaId, d.DownloadExtensionId, d.Identifier });
        
        #endregion
        
        #region DbMetadataLink

        modelBuilder.Entity<DbMetadataLink>()
            .HasKey(l => l.Id);
        
        modelBuilder.Entity<DbMetadataLink>()
            .HasOne(l => l.Cover)
            .WithOne()
            .HasForeignKey<DbMetadataLink>(d => d.CoverId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DbMetadataLink>()
            .Property(l => l.Language)
            .HasMaxLength(5);

        modelBuilder.Entity<DbMetadataLink>()
            .HasIndex(l => new { l.MangaId, l.MetadataExtensionId, l.Identifier });
        
        #endregion
    }
}