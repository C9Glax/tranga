using API.Schema.LibraryContext;
using API.Schema.LibraryContext.LibraryConnectors;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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

public enum LibraryRefreshSetting
{
    /// <summary>
    /// Refresh Libraries after all Manga are downloaded
    /// </summary>
    AfterAllFinished,
    /// <summary>
    /// Refresh Libraries after a Manga is downloaded
    /// </summary>
    AfterMangaFinished,
    /// <summary>
    /// Refresh Libraries after every download
    /// </summary>
    AfterEveryChapter,
    /// <summary>
    /// Refresh Libraries while downloading chapters, every x minutes
    /// </summary>
    WhileDownloading
}