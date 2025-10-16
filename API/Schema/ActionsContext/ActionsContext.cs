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
    }
}