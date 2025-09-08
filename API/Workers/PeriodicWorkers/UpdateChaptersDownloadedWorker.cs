using API.Schema.MangaContext;
using Microsoft.EntityFrameworkCore;

namespace API.Workers;

/// <summary>
/// Updates the database to reflect changes made on disk
/// </summary>
public class UpdateChaptersDownloadedWorker(TimeSpan? interval = null, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<MangaContext>(dependsOn), IPeriodic
{
    public DateTime LastExecution { get; set; } = DateTime.UnixEpoch;
    public TimeSpan Interval { get; set; } = interval??TimeSpan.FromMinutes(60);
    protected override async Task<BaseWorker[]> DoWorkInternal()
    {
        Log.Debug("Checking chapter files...");
        List<Chapter> chapters = await DbContext.Chapters.ToListAsync(CancellationToken);
        Log.Debug($"Checking {chapters.Count} chapters...");
        foreach (Chapter chapter in chapters)
        {
            try
            {
                chapter.Downloaded = await chapter.CheckDownloaded(DbContext, CancellationToken);
            }
            catch (Exception exception)
            {
                Log.Error(exception);
            }
        }

        if(await DbContext.Sync(CancellationToken) is { success: false } e)
            Log.Error($"Failed to save database changes: {e.exceptionMessage}");
        
        return [];
    }
}