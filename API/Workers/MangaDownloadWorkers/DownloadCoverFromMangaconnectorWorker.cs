using API.Schema.MangaContext;
using API.Schema.MangaContext.MangaConnectors;

namespace API.Workers;

public class DownloadCoverFromMangaconnectorWorker(MangaConnectorId<Manga> mcId, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<MangaContext>(dependsOn)
{
    public MangaConnectorId<Manga> MangaConnectorId { get; init; } = mcId;
    protected override BaseWorker[] DoWorkInternal()
    {
        MangaConnector mangaConnector = MangaConnectorId.MangaConnector;
        Manga manga = MangaConnectorId.Obj;
        
        manga.CoverFileNameInCache = mangaConnector.SaveCoverImageToCache(MangaConnectorId);

        DbContext.Sync();
        return [];
    }
    
    public override string ToString() => $"{base.ToString()} {MangaConnectorId}";
}