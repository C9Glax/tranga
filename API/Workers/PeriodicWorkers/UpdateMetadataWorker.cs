using API.Schema.MangaContext;
using API.Schema.MangaContext.MetadataFetchers;
using Microsoft.EntityFrameworkCore;

namespace API.Workers.PeriodicWorkers;

/// <summary>
/// Updates Metadata for all Manga
/// </summary>
/// <param name="interval"></param>
/// <param name="dependsOn"></param>
public class UpdateMetadataWorker(TimeSpan? interval = null, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<MangaContext>(dependsOn), IPeriodic
{

    public DateTime LastExecution { get; set; } = DateTime.UnixEpoch;
    public TimeSpan Interval { get; set; } = interval ?? TimeSpan.FromHours(12);
    
    protected override async Task<BaseWorker[]> DoWorkInternal()
    {
        Log.Debug("Updating metadata...");
        // Get MetadataEntries of Manga marked for download
        List<MetadataEntry> metadataEntriesToUpdate = await DbContext.MangaConnectorToManga
            .Where(m => m.UseForDownload) // Get marked Manga
            .Join(
                DbContext.MetadataEntries.Include(e => e.MetadataFetcher).Include(e => e.Manga),
                mcId => mcId.ObjId,
                e => e.MangaId,
                (mcId, e) => e) // return MetadataEntry
            .ToListAsync(CancellationToken);
        Log.Debug($"Updating metadata of {metadataEntriesToUpdate.Count} manga...");

        foreach (MetadataEntry metadataEntry in metadataEntriesToUpdate)
        {
            Log.Debug($"Updating metadata of {metadataEntry}...");
            await metadataEntry.MetadataFetcher.UpdateMetadata(metadataEntry, DbContext, CancellationToken);
        }
        Log.Debug("Updated metadata.");

        if(await DbContext.Sync(CancellationToken, GetType(), System.Reflection.MethodBase.GetCurrentMethod()?.Name) is { success: false } e)
            Log.Error($"Failed to save database changes: {e.exceptionMessage}");

        return [];
    }
}