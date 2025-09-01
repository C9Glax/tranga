using API.Schema.MangaContext;

namespace API.Workers;

public class UpdateCoversWorker(TimeSpan? interval = null, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<MangaContext>(dependsOn), IPeriodic
{

    public DateTime LastExecution { get; set; } = DateTime.UnixEpoch;
    public TimeSpan Interval { get; set; } = interval ?? TimeSpan.FromHours(6);
    
    protected override Task<BaseWorker[]> DoWorkInternal()
    {
        List<BaseWorker> workers = new();
        foreach (MangaConnectorId<Manga> mangaConnectorId in DbContext.MangaConnectorToManga)
            workers.Add(new DownloadCoverFromMangaconnectorWorker(mangaConnectorId));
        return new Task<BaseWorker[]>(() => workers.ToArray());
    }
}