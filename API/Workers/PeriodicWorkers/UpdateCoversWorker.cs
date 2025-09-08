using API.Schema.MangaContext;
using Microsoft.EntityFrameworkCore;

namespace API.Workers;

/// <summary>
/// Creates Workers to update covers for Manga
/// </summary>
/// <param name="interval"></param>
/// <param name="dependsOn"></param>
public class UpdateCoversWorker(TimeSpan? interval = null, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<MangaContext>(dependsOn), IPeriodic
{

    public DateTime LastExecution { get; set; } = DateTime.UnixEpoch;
    public TimeSpan Interval { get; set; } = interval ?? TimeSpan.FromHours(6);
    
    protected override async Task<BaseWorker[]> DoWorkInternal()
    {
        List<MangaConnectorId<Manga>> manga = await DbContext.MangaConnectorToManga.Where(mcId => mcId.UseForDownload).ToListAsync(CancellationToken);
        List<BaseWorker> newWorkers = manga.Select(m => new DownloadCoverFromMangaconnectorWorker(m)).ToList<BaseWorker>();
        return newWorkers.ToArray();
    }
}