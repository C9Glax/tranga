using API.MangaConnectors;
using API.Schema.MangaContext;

namespace API.Workers;

public class DownloadCoverFromMangaconnectorWorker(MangaConnectorId<Manga> mcId, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<MangaContext>(dependsOn)
{
    internal readonly string MangaConnectorIdId = mcId.Key;
    protected override BaseWorker[] DoWorkInternal()
    {
        if (DbContext.MangaConnectorToManga.Find(MangaConnectorIdId) is not { } mangaConnectorId)
            return []; //TODO Exception?
        if (!Tranga.TryGetMangaConnector(mangaConnectorId.MangaConnectorName, out MangaConnector? mangaConnector))
            return []; //TODO Exception?
        
        DbContext.Entry(mangaConnectorId).Navigation(nameof(MangaConnectorId<Manga>.Obj)).Load();
        Manga manga = mangaConnectorId.Obj;
        
        manga.CoverFileNameInCache = mangaConnector.SaveCoverImageToCache(mangaConnectorId);

        DbContext.Sync();
        return [];
    }
    
    public override string ToString() => $"{base.ToString()} {MangaConnectorIdId}";
}