using Microsoft.EntityFrameworkCore;

namespace Database.DownloadContext;

public class DownloadContext : DbContext
{
    public DbSet<DbManga> Mangas { get; init; }
    
    public DbSet<DbChapter> Chapters { get; init; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        #region DbManga

        modelBuilder.Entity<DbManga>()
            .HasKey(m => m.MangaId);

        modelBuilder.Entity<DbManga>()
            .HasMany(m => m.Chapters)
            .WithOne(c => c.Manga)
            .HasForeignKey(c => c.MangaId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<DbManga>()
            .HasOne(m => m.ComicInfo)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
        #endregion

        #region DbChapter
        modelBuilder.Entity<DbChapter>()
            .HasKey(c => c.ChapterId);

        modelBuilder.Entity<DbChapter>()
            .HasMany(c => c.DownloadExtensionIds)
            .WithOne(e => e.Parent)
            .HasForeignKey(e => e.ParentId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<DbChapter>()
            .HasOne(c => c.ComicInfo)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
        #endregion

        #region ExtensionId
        modelBuilder.Entity<DownloadExtensionId>()
            .HasKey(e => new { e.ExtensionIdentifier, e.Identifier });
        #endregion
    }
}