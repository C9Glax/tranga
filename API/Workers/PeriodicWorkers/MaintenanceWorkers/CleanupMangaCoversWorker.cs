using System.Diagnostics.CodeAnalysis;
using API.Schema.MangaContext;
using Microsoft.EntityFrameworkCore;

namespace API.Workers.PeriodicWorkers.MaintenanceWorkers;

public class CleanupMangaCoversWorker(TimeSpan? interval = null, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContexts(dependsOn), IPeriodic
{
    public DateTime LastExecution { get; set; } = DateTime.UnixEpoch;
    public TimeSpan Interval { get; set; } = interval ?? TimeSpan.FromHours(24);

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private MangaContext MangaContext = null!;

    protected override void SetContexts(IServiceScope serviceScope)
    {
        MangaContext = GetContext<MangaContext>(serviceScope);
    }
    
    protected override async Task<BaseWorker[]> DoWorkInternal()
    {
        Log.Info("Removing stale files...");
        string[] usedFiles = await MangaContext.Mangas.Where(m => m.CoverFileNameInCache != null).Select(m => m.CoverFileNameInCache!).ToArrayAsync(CancellationToken);
        CleanupImageCache(usedFiles, TrangaSettings.CoverImageCacheOriginal);
        CleanupImageCache(usedFiles, TrangaSettings.CoverImageCacheLarge);
        CleanupImageCache(usedFiles, TrangaSettings.CoverImageCacheMedium);
        CleanupImageCache(usedFiles, TrangaSettings.CoverImageCacheSmall);
        return [];
    }

    private void CleanupImageCache(string[] retainFilenames, string imageCachePath)
    {
        DirectoryInfo directory = new(imageCachePath);
        if (!directory.Exists)
            return;
        string[] extraneousFiles = directory
            .GetFiles()
            .Where(f => !retainFilenames.Contains(f.Name))
            .Select(f => f.FullName)
            .ToArray();
        foreach (string path in extraneousFiles)
        {
            Log.InfoFormat("Deleting {0}", path);
            File.Delete(path);
        }
    }
}