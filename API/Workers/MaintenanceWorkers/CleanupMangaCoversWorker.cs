using API.Schema.MangaContext;

namespace API.Workers.MaintenanceWorkers;

public class CleanupMangaCoversWorker(IServiceScope scope, IEnumerable<BaseWorker>? dependsOn = null) : BaseWorkerWithContext<MangaContext>(scope, dependsOn), IPeriodic<CleanupMangaCoversWorker>
{
    public DateTime LastExecution { get; set; } = DateTime.UtcNow;
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(60);
    
    protected override BaseWorker[] DoWorkInternal()
    {
        Log.Info("Removing stale files...");
        if (!Directory.Exists(TrangaSettings.coverImageCache))
            return [];
        string[] usedFiles = DbContext.Mangas.Select(m => m.CoverFileNameInCache).Where(s => s != null).ToArray()!;
        string[] extraneousFiles = new DirectoryInfo(TrangaSettings.coverImageCache).GetFiles()
            .Where(f => usedFiles.Contains(f.FullName) == false)
            .Select(f => f.FullName)
            .ToArray();
        foreach (string path in extraneousFiles)
        {
            Log.Info($"Deleting {path}");
            File.Delete(path);
        }

        return [];
    }
}