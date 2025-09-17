using API.MangaConnectors;
using API.Schema.MangaContext;
using Microsoft.EntityFrameworkCore;

namespace API.Workers.MangaDownloadWorkers;

/// <summary>
/// Downloads the cover for Manga from Mangaconnector
/// </summary>
public class DownloadCoverFromMangaconnectorWorker(MangaConnectorId<Manga> mcId, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<MangaContext>(dependsOn)
{
    internal readonly string MangaConnectorIdId = mcId.Key;
    protected override async Task<BaseWorker[]> DoWorkInternal()
    {
        Log.Debug($"Getting Cover for MangaConnectorId {MangaConnectorIdId}...");
        // Getting MangaConnector info
        if (await DbContext.MangaConnectorToManga
                .Include(id => id.Obj)
                .FirstOrDefaultAsync(c => c.Key == MangaConnectorIdId, CancellationToken) is not { } mangaConnectorId)
        {
            Log.Error("Could not get MangaConnectorId.");
            return []; //TODO Exception?
        }
        if (!Tranga.TryGetMangaConnector(mangaConnectorId.MangaConnectorName, out MangaConnector? mangaConnector))
        {
            Log.Error("Could not get MangaConnector.");
            return []; //TODO Exception?
        }
        Log.Debug($"Getting Cover for MangaConnectorId {mangaConnectorId}...");

        string? coverFileName = mangaConnector.SaveCoverImageToCache(mangaConnectorId);
        if (coverFileName is null)
        {
            Log.Error($"Could not get Cover for MangaConnectorId {mangaConnectorId}.");
            return [];
        }
        DbContext.Entry(mangaConnectorId.Obj).Property(m => m.CoverFileNameInCache).CurrentValue = coverFileName;

        if(await DbContext.Sync(CancellationToken) is { success: false } e)
            Log.Error($"Failed to save database changes: {e.exceptionMessage}");
        
        return [];
    }
    
    public override string ToString() => $"{base.ToString()} {MangaConnectorIdId}";
}