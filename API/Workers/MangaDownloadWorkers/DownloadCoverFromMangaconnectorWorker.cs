using API.MangaConnectors;
using API.Schema.MangaContext;
using Microsoft.EntityFrameworkCore;

namespace API.Workers;

public class DownloadCoverFromMangaconnectorWorker(MangaConnectorId<Manga> mcId, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<MangaContext>(dependsOn)
{
    internal readonly string MangaConnectorIdId = mcId.Key;
    protected override async Task<BaseWorker[]> DoWorkInternal()
    {
        if (await DbContext.MangaConnectorToManga.FirstOrDefaultAsync(c => c.Key == MangaConnectorIdId) is not { } mangaConnectorId)
            return []; //TODO Exception?
        if (!Tranga.TryGetMangaConnector(mangaConnectorId.MangaConnectorName, out MangaConnector? mangaConnector))
            return []; //TODO Exception?
        
        await DbContext.Entry(mangaConnectorId).Navigation(nameof(MangaConnectorId<Manga>.Obj)).LoadAsync(CancellationTokenSource.Token);
        Manga manga = mangaConnectorId.Obj;
        
        manga.CoverFileNameInCache = mangaConnector.SaveCoverImageToCache(mangaConnectorId);

        await DbContext.Sync(CancellationTokenSource.Token);
        return [];
    }
    
    public override string ToString() => $"{base.ToString()} {MangaConnectorIdId}";
}