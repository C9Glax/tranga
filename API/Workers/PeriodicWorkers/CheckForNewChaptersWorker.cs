using API.Schema.MangaContext;

namespace API.Workers;

public class CheckForNewChaptersWorker(TimeSpan? interval = null, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<MangaContext>(dependsOn), IPeriodic
{
    public DateTime LastExecution { get; set; } = DateTime.UtcNow;
    public TimeSpan Interval { get; set; } = interval??TimeSpan.FromMinutes(60);
    
    protected override BaseWorker[] DoWorkInternal()
    {
        IQueryable<MangaConnectorId<Manga>> connectorIdsManga = DbContext.MangaConnectorToManga.Where(id => id.UseForDownload);

        List<BaseWorker> newWorkers = new();
        foreach (MangaConnectorId<Manga> mangaConnectorId in connectorIdsManga)
            newWorkers.Add(new RetrieveMangaChaptersFromMangaconnectorWorker(mangaConnectorId, Tranga.Settings.DownloadLanguage));
        
        return newWorkers.ToArray();
    }

}