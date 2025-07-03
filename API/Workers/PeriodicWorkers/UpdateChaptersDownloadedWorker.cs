using API.Schema.MangaContext;
namespace API.Workers;

public class UpdateChaptersDownloadedWorker(Manga manga, TimeSpan? interval = null, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<MangaContext>(dependsOn), IPeriodic
{
    public DateTime LastExecution { get; set; } = DateTime.UtcNow;
    public TimeSpan Interval { get; set; } = interval??TimeSpan.FromMinutes(60);
    protected override BaseWorker[] DoWorkInternal()
    {
        foreach (Chapter mangaChapter in manga.Chapters)
        {
            mangaChapter.Downloaded = mangaChapter.CheckDownloaded();
        }

        DbContext.Sync();
        return [];
    }

    public override string ToString() => $"{base.ToString()} {manga}";
}