using System.Diagnostics.CodeAnalysis;
using API.Schema.MangaContext;
using API.Workers.MangaDownloadWorkers;
using Microsoft.EntityFrameworkCore;

namespace API.Workers.PeriodicWorkers;

/// <summary>
/// Creates Workers to update covers for Manga
/// </summary>
/// <param name="interval"></param>
/// <param name="dependsOn"></param>
public class UpdateCoversWorker(TimeSpan? interval = null, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContexts(dependsOn), IPeriodic
{
    public DateTime LastExecution { get; set; } = DateTime.UnixEpoch;
    public TimeSpan Interval { get; set; } = interval ?? TimeSpan.FromHours(6);
    
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private MangaContext MangaContext = null!;

    protected override void SetContexts(IServiceScope serviceScope)
    {
        MangaContext = GetContext<MangaContext>(serviceScope);
    }
    
    protected override async Task<BaseWorker[]> DoWorkInternal()
    {
        List<MangaConnectorId<Manga>> manga = await MangaContext.MangaConnectorToManga.Where(mcId => mcId.UseForDownload).ToListAsync(CancellationToken);
        List<BaseWorker> newWorkers = manga.Select(m => new DownloadCoverFromMangaconnectorWorker(m)).ToList<BaseWorker>();
        return newWorkers.ToArray();
    }
}