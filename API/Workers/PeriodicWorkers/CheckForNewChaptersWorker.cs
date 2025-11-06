using System.Diagnostics.CodeAnalysis;
using API.Schema.MangaContext;
using API.Workers.MangaDownloadWorkers;
using Microsoft.EntityFrameworkCore;

namespace API.Workers.PeriodicWorkers;

/// <summary>
/// Creates Jobs to update available Chapters for all Manga that are marked for Download
/// </summary>
public class CheckForNewChaptersWorker(TimeSpan? interval = null, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContexts(dependsOn), IPeriodic
{
    public DateTime LastExecution { get; set; } = DateTime.UnixEpoch;
    public TimeSpan Interval { get; set; } = interval??Constants.CheckForNewChaptersInterval;
    
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private MangaContext MangaContext = null!;

    protected override void SetContexts(IServiceScope serviceScope)
    {
        MangaContext = GetContext<MangaContext>(serviceScope);
    }
    
    protected override async Task<BaseWorker[]> DoWorkInternal()
    {
        Log.Debug("Checking for new chapters...");
        List<MangaConnectorId<Manga>> connectorIdsManga = await MangaContext.MangaConnectorToManga
            .Include(id => id.Obj)
            .Where(id => id.UseForDownload)
            .ToListAsync(CancellationToken);
        Log.DebugFormat("Creating {0} update jobs...", connectorIdsManga.Count);

        List<BaseWorker> newWorkers = connectorIdsManga.Select(id => new RetrieveMangaChaptersFromMangaconnectorWorker(id, Tranga.Settings.DownloadLanguage))
            .ToList<BaseWorker>();

        return newWorkers.ToArray();
    }
}