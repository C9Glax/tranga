using API.Schema.MangaContext;
using Microsoft.EntityFrameworkCore;

namespace API.Workers;

public class UpdateChaptersDownloadedWorker(Manga manga, IServiceScope scope, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<MangaContext>(scope, dependsOn), IPeriodic
{
    public DateTime LastExecution { get; set; } = DateTime.UtcNow;
    public TimeSpan Interval { get; set; } =  TimeSpan.FromMinutes(60);
    protected override BaseWorker[] DoWorkInternal()
    {
        foreach (Chapter mangaChapter in manga.Chapters)
        {
            mangaChapter.Downloaded = mangaChapter.CheckDownloaded();
        }

        try
        {
            DbContext.SaveChanges();
        }
        catch (DbUpdateException e)
        {
            Log.Error(e);
        }
        return [];
    }
}