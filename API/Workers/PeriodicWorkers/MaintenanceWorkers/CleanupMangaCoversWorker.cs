using API.Schema.MangaContext;

namespace API.Workers.PeriodicWorkers.MaintenanceWorkers;

public class CleanupMangaCoversWorker(TimeSpan? interval = null, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<MangaContext>(dependsOn), IPeriodic
{
    public DateTime LastExecution { get; set; } = DateTime.UnixEpoch;
    public TimeSpan Interval { get; set; } = interval ?? TimeSpan.FromHours(24);
    
    protected override Task<BaseWorker[]> DoWorkInternal()
    {
        Log.Info("Removing stale files...");
        string[] usedFiles = DbContext.Mangas.Select(m => m.CoverFileNameInCache).Where(s => s != null).ToArray()!;
        CleanupImageCache(usedFiles, TrangaSettings.CoverImageCacheOriginal);
        CleanupImageCache(usedFiles, TrangaSettings.CoverImageCacheLarge);
        CleanupImageCache(usedFiles, TrangaSettings.CoverImageCacheMedium);
        CleanupImageCache(usedFiles, TrangaSettings.CoverImageCacheSmall);
        return new Task<BaseWorker[]>(() => []);
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