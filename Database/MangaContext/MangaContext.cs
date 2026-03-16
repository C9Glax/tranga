using Microsoft.EntityFrameworkCore;

namespace Database.MangaContext;

public class MangaContext(DbContextOptions<MangaContext> options) : TrangaDatabaseContext<MangaContext>(options)
{
    public DbSet<DbManga> Mangas { get; init; }
    
    public DbSet<DbChapter> Chapters { get; init; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        #region DbManga
        modelBuilder.Entity<DbManga>()
            .ToTable("Manga")
            .HasKey(m => m.MangaId);

        modelBuilder.Entity<DbManga>()
            .HasMany(m => m.Chapters)
            .WithOne(c => c.Manga)
            .HasForeignKey(c => c.MangaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DbManga>()
            .HasMany(m => m.DownloadExtensionIds)
            .WithOne(e => e.Parent)
            .HasForeignKey(e => e.ParentId)
            .OnDelete(DeleteBehavior.Cascade);
        #endregion
        
        #region DbChapter
        modelBuilder.Entity<DbChapter>()
            .ToTable("Chapter")
            .HasKey(c => c.ChapterId);

        modelBuilder.Entity<DbChapter>()
            .HasMany(c => c.DownloadExtensionIds)
            .WithOne(e => e.Parent)
            .HasForeignKey(e => e.ParentId)
            .OnDelete(DeleteBehavior.Cascade);
        #endregion


        #region ExtensionId
        modelBuilder.Entity<DownloadExtensionId<DbManga>>()
            .ToTable("MangaDownloadExtensionIds")
            .HasKey(dei => new { dei.ParentId, dei.Identifier });
        modelBuilder.Entity<DownloadExtensionId<DbManga>>()
            .HasOne(e => e.Parent)
            .WithMany()
            .HasForeignKey(e => e.ParentId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<DownloadExtensionId<DbChapter>>()
            .ToTable("ChapterDownloadExtensionIds")
            .HasKey(dei => new { dei.ParentId, dei.Identifier });
        modelBuilder.Entity<DownloadExtensionId<DbChapter>>()
            .HasOne(e => e.Parent)
            .WithMany(c => c.DownloadExtensionIds)
            .HasForeignKey(e => e.ParentId)
            .OnDelete(DeleteBehavior.Cascade);
        #endregion
    }
}