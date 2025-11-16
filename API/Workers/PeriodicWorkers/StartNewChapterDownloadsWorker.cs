using System.Diagnostics.CodeAnalysis;
using API.Schema.MangaContext;
using API.Workers.MangaDownloadWorkers;
using Microsoft.EntityFrameworkCore;

namespace API.Workers.PeriodicWorkers;

/// <summary>
/// Create new Workers for Chapters on Manga marked for Download, that havent been downloaded yet.
/// </summary>
public class StartNewChapterDownloadsWorker(TimeSpan? interval = null, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContexts(dependsOn), IPeriodic
{

    public DateTime LastExecution { get; set; } = DateTime.UnixEpoch;
    public TimeSpan Interval { get; set; } = interval ?? TimeSpan.FromMinutes(1);
    
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private MangaContext MangaContext = null!;

    protected override void SetContexts(IServiceScope serviceScope)
    {
        MangaContext = GetContext<MangaContext>(serviceScope);
    }
    
    protected override async Task<BaseWorker[]> DoWorkInternal()
    {
        Log.Debug("Checking for missing chapters...");
        
        // Get missing chapters
        List<MangaConnectorId<Chapter>> missingChapters = await GetMissingChapters(MangaContext, CancellationToken);
        
        Log.DebugFormat("Found {0} missing chapters.", missingChapters.Count);
        List<string> chaptersDownloading = Tranga.GetRunningWorkers()
            .Where(w => w is DownloadChapterFromMangaconnectorWorker)
            .Select(w => ((DownloadChapterFromMangaconnectorWorker)w).ChapterIdId).ToList();
        missingChapters.RemoveAll(ch => chaptersDownloading.Contains(ch.Key));
        Log.DebugFormat("{0} chapter not being downloaded", missingChapters.Count);
        
        // Maximum Concurrent workers
        int downloadWorkers = Tranga.GetRunningWorkers().Count(w => w.GetType() == typeof(DownloadChapterFromMangaconnectorWorker));
        int amountNewWorkers = Math.Min(Tranga.Settings.MaxConcurrentDownloads, Tranga.Settings.MaxConcurrentDownloads - downloadWorkers);
        
        Log.DebugFormat("{0} running download Workers. {1} available new download Workers.", downloadWorkers, amountNewWorkers);
        IEnumerable<MangaConnectorId<Chapter>> newDownloadChapters = missingChapters.OrderBy(ch => ch.Obj, new Chapter.ChapterComparer()).Take(amountNewWorkers);

        // Create new jobs
        List<BaseWorker> newWorkers = newDownloadChapters.Select(mcId => new DownloadChapterFromMangaconnectorWorker(mcId)).ToList<BaseWorker>();
        
        return newWorkers.ToArray();
    }
    
    internal static async Task<List<MangaConnectorId<Chapter>>> GetMissingChapters(MangaContext ctx, CancellationToken cancellationToken) => await ctx.MangaConnectorToChapter
        .Include(id => id.Obj)
        .Where(id => !id.Obj.Downloaded && id.UseForDownload)
        .ToListAsync(cancellationToken);
}