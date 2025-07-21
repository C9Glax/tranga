using API.Schema.MangaContext;
using Microsoft.EntityFrameworkCore;

namespace API.Workers;

public class UpdateChaptersDownloadedWorker(TimeSpan? interval = null, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<MangaContext>(dependsOn), IPeriodic
{
    public DateTime LastExecution { get; set; } = DateTime.UnixEpoch;
    public TimeSpan Interval { get; set; } = interval??TimeSpan.FromMinutes(60);
    protected override BaseWorker[] DoWorkInternal()
    {
        foreach (Chapter dbContextChapter in DbContext.Chapters.Include(c => c.ParentManga))
            dbContextChapter.Downloaded = dbContextChapter.CheckDownloaded();

        DbContext.Sync();
        return [];
    }
}