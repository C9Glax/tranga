using System.Diagnostics.CodeAnalysis;
using API.Schema.MangaContext;
using Microsoft.EntityFrameworkCore;

namespace API.Workers.PeriodicWorkers;

/// <summary>
/// Updates the database to reflect changes made on disk
/// </summary>
public class UpdateChaptersDownloadedWorker(TimeSpan? interval = null, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContexts(dependsOn), IPeriodic
{
    public DateTime LastExecution { get; set; } = DateTime.UnixEpoch;
    public TimeSpan Interval { get; set; } = interval??TimeSpan.FromDays(1);
    
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private MangaContext MangaContext = null!;

    protected override void SetContexts(IServiceScope serviceScope)
    {
        MangaContext = GetContext<MangaContext>(serviceScope);
    }
    
    protected override async Task<BaseWorker[]> DoWorkInternal()
    {
        Log.Debug("Checking chapter files...");
        List<Chapter> chapters = await MangaContext.Chapters.ToListAsync(CancellationToken);
        Log.Debug($"Checking {chapters.Count} chapters...");
        foreach (Chapter chapter in chapters)
        {
            try
            {
                chapter.Downloaded = await chapter.CheckDownloaded(MangaContext, CancellationToken);
            }
            catch (Exception exception)
            {
                Log.Error(exception);
            }
        }

        if(await MangaContext.Sync(CancellationToken, GetType(), System.Reflection.MethodBase.GetCurrentMethod()?.Name) is { success: false } e)
            Log.Error($"Failed to save database changes: {e.exceptionMessage}");
        
        return [];
    }
}