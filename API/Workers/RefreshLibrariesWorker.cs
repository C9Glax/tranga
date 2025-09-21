using API.Schema.LibraryContext;
using API.Schema.LibraryContext.LibraryConnectors;
using Microsoft.EntityFrameworkCore;

namespace API.Workers;

public class RefreshLibrariesWorker(IEnumerable<BaseWorker>? dependsOn = null) : BaseWorkerWithContext<LibraryContext>(dependsOn)
{
    public static DateTime LastRefresh { get; set; } = DateTime.UnixEpoch;
    
    protected override async Task<BaseWorker[]> DoWorkInternal()
    {
        Log.Debug("Refreshing libraries...");
        LastRefresh = DateTime.UtcNow;
        List<LibraryConnector> libraries = await DbContext.LibraryConnectors.ToListAsync(CancellationToken);
        foreach (LibraryConnector connector in libraries)
            await connector.UpdateLibrary(CancellationToken);
        Log.Debug("Libraries Refreshed...");
        return [];
    }
}

public enum LibraryRefreshSetting : byte
{
    AfterAllFinished = 0,
    AfterMangaFinished = 1,
    AfterEveryChapter = 2,
    WhileDownloading = 3
}