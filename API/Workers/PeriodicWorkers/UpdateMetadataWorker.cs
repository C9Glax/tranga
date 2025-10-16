using System.Diagnostics.CodeAnalysis;
using API.Schema.ActionsContext;
using API.Schema.ActionsContext.Actions;
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
    : BaseWorkerWithContexts(dependsOn), IPeriodic
{

    public DateTime LastExecution { get; set; } = DateTime.UnixEpoch;
    public TimeSpan Interval { get; set; } = interval ?? TimeSpan.FromHours(12);

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private MangaContext MangaContext = null!;
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private ActionsContext ActionsContext = null!;

    protected override void SetContexts(IServiceScope serviceScope)
    {
        MangaContext = GetContext<MangaContext>(serviceScope);
        ActionsContext = GetContext<ActionsContext>(serviceScope);
    }
    
    protected override async Task<BaseWorker[]> DoWorkInternal()
    {
        Log.Debug("Updating metadata...");
        // Get MetadataEntries of Manga marked for download
        List<MetadataEntry> metadataEntriesToUpdate = await MangaContext.MangaConnectorToManga
            .Where(m => m.UseForDownload) // Get marked Manga
            .Join(
                MangaContext.MetadataEntries.Include(e => e.MetadataFetcher).Include(e => e.Manga),
                mcId => mcId.ObjId,
                e => e.MangaId,
                (mcId, e) => e) // return MetadataEntry
            .ToListAsync(CancellationToken);
        Log.Debug($"Updating metadata of {metadataEntriesToUpdate.Count} manga...");

        foreach (MetadataEntry metadataEntry in metadataEntriesToUpdate)
        {
            Log.Debug($"Updating metadata of {metadataEntry}...");
            await metadataEntry.MetadataFetcher.UpdateMetadata(metadataEntry, MangaContext, CancellationToken);
            ActionsContext.Actions.Add(new MetadataUpdatedActionRecord(metadataEntry.Manga, metadataEntry.MetadataFetcher));
        }
        Log.Debug("Updated metadata.");

        if(await MangaContext.Sync(CancellationToken, GetType(), System.Reflection.MethodBase.GetCurrentMethod()?.Name) is { success: false } e)
            Log.Error($"Failed to save database changes: {e.exceptionMessage}");
        
        if(await ActionsContext.Sync(CancellationToken, GetType(), "Metadata Updated") is { success: false } actionsContextException)
            Log.Error($"Failed to save database changes: {actionsContextException.exceptionMessage}");

        return [];
    }
}