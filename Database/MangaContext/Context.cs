using Microsoft.EntityFrameworkCore;

namespace Database.MangaContext;

public class Context : DbContext
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
            .HasMany(m => m.ExtensionIds)
            .WithOne(e => e.Parent)
            .HasForeignKey(e => e.ParentId)
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
            .HasMany(c => c.ExtensionIds)
            .WithOne(e => e.Parent)
            .HasForeignKey(e => e.ParentId)
            .OnDelete(DeleteBehavior.Cascade);
        
        modelBuilder.Entity<DbChapter>()
            .HasOne(c => c.ComicInfo)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);
        #endregion

        #region ExtensionId
        modelBuilder.Entity<ExtensionId<IRef>>()
            .HasKey(e => new { e.ExtensionIdentifier, e.Identifier });
        #endregion
    }
}