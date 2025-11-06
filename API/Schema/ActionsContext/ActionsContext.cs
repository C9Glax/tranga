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
            .HasValue<ChapterDownloadedActionRecord>(Schema.ActionsContext.Actions.Actions.ChapterDownloaded)
            .HasValue<CoverDownloadedActionRecord>(Schema.ActionsContext.Actions.Actions.CoverDownloaded)
            .HasValue<ChaptersRetrievedActionRecord>(Schema.ActionsContext.Actions.Actions.ChaptersRetrieved)
            .HasValue<MetadataUpdatedActionRecord>(Schema.ActionsContext.Actions.Actions.MetadataUpdated)
            .HasValue<DataMovedActionRecord>(Schema.ActionsContext.Actions.Actions.DataMoved)
            .HasValue<LibraryMovedActionRecord>(Schema.ActionsContext.Actions.Actions.LibraryMoved)
            .HasValue<StartupActionRecord>(Schema.ActionsContext.Actions.Actions.Startup);

        modelBuilder.Entity<ChapterDownloadedActionRecord>().Property(a => a.MangaId).HasColumnName("MangaId");
        modelBuilder.Entity<ChapterDownloadedActionRecord>().Property(a => a.ChapterId).HasColumnName("ChapterId");
        
        modelBuilder.Entity<CoverDownloadedActionRecord>().Property(a => a.MangaId).HasColumnName("MangaId");
        
        modelBuilder.Entity<ChaptersRetrievedActionRecord>().Property(a => a.MangaId).HasColumnName("MangaId");
        
        modelBuilder.Entity<MetadataUpdatedActionRecord>().Property(a => a.MangaId).HasColumnName("MangaId");
        
        modelBuilder.Entity<LibraryMovedActionRecord>().Property(a => a.MangaId).HasColumnName("MangaId");
    }

    public IQueryable<ActionRecord> FilterActionsManga(string MangaId) => this.Actions
        .FromSqlInterpolated($"""SELECT * FROM public."Actions" WHERE "MangaId" = {MangaId}""");

    public IQueryable<ActionRecord> FilterActionsChapter(string ChapterId) => this.Actions
        .FromSqlInterpolated($"""SELECT * FROM public."Actions" WHERE "ChapterId" = {ChapterId}""");
    
    public IQueryable<ActionRecord> FilterActionsMangaAndChapter(string MangaId, string ChapterId) => this.Actions
        .FromSqlInterpolated($"""SELECT * FROM public."Actions" WHERE "MangaId" = {MangaId} AND "ChapterId" = {ChapterId}""");

    public IQueryable<ActionRecord> FilterActions(string? MangaId, string? ChapterId)
    {
        if (MangaId is { } mangaId && ChapterId is { } chapterId)
            return FilterActionsMangaAndChapter(mangaId, chapterId);
        if (MangaId is { } mangaId2)
            return FilterActionsManga(mangaId2);
        if (ChapterId is { } chapterId2)
            return FilterActionsChapter(chapterId2);
        return this.Actions.AsQueryable();
    }
}