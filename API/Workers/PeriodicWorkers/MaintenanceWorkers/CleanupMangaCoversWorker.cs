using API.Schema.MangaContext;

namespace API.Workers.MaintenanceWorkers;

public class CleanupMangaCoversWorker(TimeSpan? interval = null, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<MangaContext>(dependsOn), IPeriodic
{
    public DateTime LastExecution { get; set; } = DateTime.UnixEpoch;
    public TimeSpan Interval { get; set; } = interval ?? TimeSpan.FromHours(24);
    
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