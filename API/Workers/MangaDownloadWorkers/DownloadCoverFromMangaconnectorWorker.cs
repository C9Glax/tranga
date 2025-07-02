using API.Schema.MangaContext;
using API.Schema.MangaContext.MangaConnectors;
using Microsoft.EntityFrameworkCore;

namespace API.Workers;

public class DownloadCoverFromMangaconnectorWorker(MangaConnectorId<Manga> mcId, IServiceScope scope, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<MangaContext>(scope, dependsOn)
{
    public MangaConnectorId<Manga> MangaConnectorId { get; init; } = mcId;
    protected override BaseWorker[] DoWorkInternal()
    {
        MangaConnector mangaConnector = MangaConnectorId.MangaConnector;
        Manga manga = MangaConnectorId.Obj;
        try
        {
            manga.CoverFileNameInCache = mangaConnector.SaveCoverImageToCache(MangaConnectorId);
            DbContext.SaveChanges();
        }
        catch (DbUpdateException e)
        {
            Log.Error(e);
        }
        return [];
    }
}