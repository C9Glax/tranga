using API.Schema.MangaContext;
using Microsoft.EntityFrameworkCore;

namespace API.Workers;

/// <summary>
/// Create new Workers for Chapters on Manga marked for Download, that havent been downloaded yet.
/// </summary>
public class StartNewChapterDownloadsWorker(TimeSpan? interval = null, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<MangaContext>(dependsOn), IPeriodic
{

    public DateTime LastExecution { get; set; } = DateTime.UnixEpoch;
    public TimeSpan Interval { get; set; } = interval ?? TimeSpan.FromMinutes(1);
    protected override async Task<BaseWorker[]> DoWorkInternal()
    {
        Log.Debug("Checking for missing chapters...");
        
        // Get missing chapters
        List<MangaConnectorId<Chapter>> missingChapters = await DbContext.MangaConnectorToChapter
            .Include(id => id.Obj)
            .Where(id => id.Obj.Downloaded == false && id.UseForDownload)
            .ToListAsync(CancellationToken);
        
        Log.Debug($"Found {missingChapters.Count} missing downloads.");
        
        // Maximum Concurrent workers
        int downloadWorkers = Tranga.GetRunningWorkers().Count(w => w.GetType() == typeof(DownloadChapterFromMangaconnectorWorker));
        int amountNewWorkers = Math.Min(Tranga.Settings.MaxConcurrentDownloads, Tranga.Settings.MaxConcurrentDownloads - downloadWorkers);
        
        Log.Debug($"{downloadWorkers} running download Workers. {amountNewWorkers} new download Workers.");
        IEnumerable<MangaConnectorId<Chapter>> newDownloadChapters = missingChapters.OrderBy(ch => ch.Obj, new Chapter.ChapterComparer()).Take(amountNewWorkers);

        // Create new jobs
        List<BaseWorker> newWorkers = newDownloadChapters.Select(mcId => new DownloadChapterFromMangaconnectorWorker(mcId)).ToList<BaseWorker>();
        
        return newWorkers.ToArray();
    }
}