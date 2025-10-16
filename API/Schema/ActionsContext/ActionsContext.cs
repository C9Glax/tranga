using API.Schema.ActionsContext.Actions;
using Microsoft.EntityFrameworkCore;

namespace API.Schema.ActionsContext;

public class ActionsContext(DbContextOptions<ActionsContext> options) : TrangaBaseContext<ActionsContext>(options)
{
    public DbSet<ActionRecord> Actions  { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActionRecord>()
            .HasDiscriminator(a => a.Action)
            .HasValue<ChapterDownloadedActionRecord>(ChapterDownloadedActionRecord.ChapterDownloadedAction)
            .HasValue<CoverDownloadedActionRecord>(CoverDownloadedActionRecord.CoverDownloadedAction)
            .HasValue<ChaptersRetrievedActionRecord>(ChaptersRetrievedActionRecord.ChaptersRetrievedAction)
            .HasValue<MetadataUpdatedActionRecord>(MetadataUpdatedActionRecord.MetadataUpdatedAction)
            .HasValue<DataMovedActionRecord>(DataMovedActionRecord.DataMovedAction)
            .HasValue<LibraryMovedActionRecord>(LibraryMovedActionRecord.LibraryMovedAction)
            .HasValue<StartupActionRecord>(StartupActionRecord.StartupAction);
    }
}