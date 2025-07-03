using API.Schema.MangaContext;

namespace API.Workers;

public class StartNewChapterDownloadsWorker(TimeSpan? interval = null, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<MangaContext>(dependsOn), IPeriodic
{

    public DateTime LastExecution { get; set; } = DateTime.UnixEpoch;
    public TimeSpan Interval { get; set; } = interval ?? TimeSpan.FromMinutes(120);
    protected override BaseWorker[] DoWorkInternal()
    {
        IQueryable<MangaConnectorId<Chapter>> mangaConnectorIds = DbContext.MangaConnectorToChapter.Where(id => id.Obj.Downloaded == false && id.UseForDownload);
        
        List<BaseWorker> newWorkers = new();
        foreach (MangaConnectorId<Chapter> mangaConnectorId in mangaConnectorIds)
            newWorkers.Add(new DownloadChapterFromMangaconnectorWorker(mangaConnectorId));
        
        return newWorkers.ToArray();
    }
}