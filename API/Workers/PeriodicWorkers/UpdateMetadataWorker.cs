using API.Schema.MangaContext;
using API.Schema.MangaContext.MetadataFetchers;
using Microsoft.EntityFrameworkCore;

namespace API.Workers;

public class UpdateMetadataWorker(TimeSpan? interval = null, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<MangaContext>(dependsOn), IPeriodic
{

    public DateTime LastExecution { get; set; } = DateTime.UnixEpoch;
    public TimeSpan Interval { get; set; } = interval ?? TimeSpan.FromHours(12);
    
    protected override async Task<BaseWorker[]> DoWorkInternal()
    {
        IQueryable<string> mangaIds = DbContext.MangaConnectorToManga
            .Where(m => m.UseForDownload)
            .Select(m => m.ObjId);
        IQueryable<MetadataEntry> metadataEntriesToUpdate = DbContext.MetadataEntries
            .Include(e => e.MetadataFetcher)
            .Where(e =>
                mangaIds.Any(id => id == e.MangaId));
        
        foreach (MetadataEntry metadataEntry in metadataEntriesToUpdate)
            await metadataEntry.MetadataFetcher.UpdateMetadata(metadataEntry, DbContext, CancellationTokenSource.Token);

        await DbContext.Sync(CancellationTokenSource.Token);

        return [];
    }
}