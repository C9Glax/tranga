using API.Schema.MangaContext;

namespace API.Workers;

public class CheckForNewChaptersWorker(Manga manga, TimeSpan? interval = null, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<MangaContext>(dependsOn), IPeriodic
{
    public Manga Manga = manga;
    public DateTime LastExecution { get; set; } = DateTime.UtcNow;
    public TimeSpan Interval { get; set; } = interval??TimeSpan.FromMinutes(60);
    
    protected override BaseWorker[] DoWorkInternal()
    {
        ICollection<MangaConnectorId<Manga>> connectorIdsManga = Manga.MangaConnectorIds;
        IEnumerable<MangaConnectorId<Manga>> mangasToDownload = connectorIdsManga.Where(id => id.UseForDownload);

        List<BaseWorker> newWorkers = new();
        foreach (MangaConnectorId<Manga> mangaConnectorId in mangasToDownload)
            newWorkers.Add(new RetrieveMangaChaptersFromMangaconnectorWorker(mangaConnectorId, Tranga.Settings.DownloadLanguage));
        
        return newWorkers.ToArray();
    }

}