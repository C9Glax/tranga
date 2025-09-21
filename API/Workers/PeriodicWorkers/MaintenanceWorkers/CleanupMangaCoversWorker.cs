using API.Schema.MangaContext;
using Microsoft.EntityFrameworkCore;

namespace API.Workers.PeriodicWorkers.MaintenanceWorkers;

public class CleanupMangaCoversWorker(TimeSpan? interval = null, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<MangaContext>(dependsOn), IPeriodic
{
    public DateTime LastExecution { get; set; } = DateTime.UnixEpoch;
    public TimeSpan Interval { get; set; } = interval ?? TimeSpan.FromHours(24);
    
    protected override async Task<BaseWorker[]> DoWorkInternal()
    {
        Log.Info("Removing stale files...");
        string[] usedFiles = await DbContext.Mangas.Where(m => m.CoverFileNameInCache != null).Select(m => m.CoverFileNameInCache!).ToArrayAsync(CancellationToken);
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
            .Where(f => retainFilenames.Contains(f.Name) == false)
            .Select(f => f.FullName)
            .ToArray();
        foreach (string path in extraneousFiles)
        {
            Log.Info($"Deleting {path}");
            File.Delete(path);
        }
    }
}