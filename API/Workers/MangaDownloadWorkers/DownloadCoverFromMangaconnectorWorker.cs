using System.Diagnostics.CodeAnalysis;
using API.MangaConnectors;
using API.Schema.ActionsContext;
using API.Schema.ActionsContext.Actions;
using API.Schema.MangaContext;
using Microsoft.EntityFrameworkCore;

namespace API.Workers.MangaDownloadWorkers;

/// <summary>
/// Downloads the cover for Manga from Mangaconnector
/// </summary>
public class DownloadCoverFromMangaconnectorWorker(MangaConnectorId<Manga> mcId, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContexts(dependsOn)
{
    private readonly string _mangaConnectorIdId = mcId.Key;

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
        Log.Debug($"Getting Cover for MangaConnectorId {_mangaConnectorIdId}...");
        // Getting MangaConnector info
        if (await MangaContext.MangaConnectorToManga
                .Include(id => id.Obj)
                .FirstOrDefaultAsync(c => c.Key == _mangaConnectorIdId, CancellationToken) is not { } mangaConnectorId)
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
        
        await MangaContext.Entry(mangaConnectorId).Reference(m => m.Obj).LoadAsync(CancellationToken);
        mangaConnectorId.Obj.CoverFileNameInCache = coverFileName;

        if(await MangaContext.Sync(CancellationToken, GetType(), System.Reflection.MethodBase.GetCurrentMethod()?.Name) is { success: false } mangaContextException)
            Log.Error($"Failed to save database changes: {mangaContextException.exceptionMessage}");
        
        ActionsContext.Actions.Add(new CoverDownloadedActionRecord(mcId.Obj, coverFileName));
        if(await ActionsContext.Sync(CancellationToken, GetType(), "Download complete") is { success: false } actionsContextException)
            Log.Error($"Failed to save database changes: {actionsContextException.exceptionMessage}");
        
        return [];
    }
    
    public override string ToString() => $"{base.ToString()} {_mangaConnectorIdId}";
}