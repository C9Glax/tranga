using API.Schema.ActionsContext.Actions;
using API.Schema.ActionsContext.Actions.Generic;
using Microsoft.EntityFrameworkCore;

namespace API.Schema.ActionsContext;

public class ActionsContext(DbContextOptions<ActionsContext> options) : TrangaBaseContext<ActionsContext>(options)
{
    public DbSet<ActionRecord> Actions  { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActionRecord>()
            .HasDiscriminator(a => a.Action)
            .HasValue<ChapterDownloadedActionRecord>(ActionsEnum.ChapterDownloaded)
            .HasValue<CoverDownloadedActionRecord>(ActionsEnum.CoverDownloaded)
            .HasValue<ChaptersRetrievedActionRecord>(ActionsEnum.ChaptersRetrieved)
            .HasValue<MetadataUpdatedActionRecord>(ActionsEnum.MetadataUpdated)
            .HasValue<DataMovedActionRecord>(ActionsEnum.DataMoved)
            .HasValue<LibraryMovedActionRecord>(ActionsEnum.LibraryMoved)
            .HasValue<StartupActionRecord>(ActionsEnum.Startup);

        modelBuilder.Entity<ChapterDownloadedActionRecord>().Property(a => a.MangaId).HasColumnName("MangaId");
        modelBuilder.Entity<ChapterDownloadedActionRecord>().Property(a => a.ChapterId).HasColumnName("ChapterId");
        
        modelBuilder.Entity<CoverDownloadedActionRecord>().Property(a => a.MangaId).HasColumnName("MangaId");
        
        modelBuilder.Entity<ChaptersRetrievedActionRecord>().Property(a => a.MangaId).HasColumnName("MangaId");
        
        modelBuilder.Entity<MetadataUpdatedActionRecord>().Property(a => a.MangaId).HasColumnName("MangaId");
        
        modelBuilder.Entity<LibraryMovedActionRecord>().Property(a => a.MangaId).HasColumnName("MangaId");
    }
}