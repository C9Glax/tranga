using API.Schema.MangaContext;
using Microsoft.EntityFrameworkCore;

namespace API.Workers;

public class StartNewChapterDownloadsWorker(TimeSpan? interval = null, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<MangaContext>(dependsOn), IPeriodic
{

    public DateTime LastExecution { get; set; } = DateTime.UnixEpoch;
    public TimeSpan Interval { get; set; } = interval ?? TimeSpan.FromMinutes(1);
    protected override Task<BaseWorker[]> DoWorkInternal()
    {
        IQueryable<MangaConnectorId<Chapter>> mangaConnectorIds = DbContext.MangaConnectorToChapter
            .Include(id => id.Obj)
            .Where(id => id.Obj.Downloaded == false && id.UseForDownload);
        
        List<BaseWorker> newWorkers = new();
        foreach (MangaConnectorId<Chapter> mangaConnectorId in mangaConnectorIds)
            newWorkers.Add(new DownloadChapterFromMangaconnectorWorker(mangaConnectorId));
        
        return new Task<BaseWorker[]>(() => newWorkers.ToArray());
    }
}