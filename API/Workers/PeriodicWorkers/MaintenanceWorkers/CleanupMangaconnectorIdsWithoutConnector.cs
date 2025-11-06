using System.Diagnostics.CodeAnalysis;
using API.Schema.MangaContext;
using Microsoft.EntityFrameworkCore;

namespace API.Workers.PeriodicWorkers.MaintenanceWorkers;

public class CleanupMangaconnectorIdsWithoutConnector : BaseWorkerWithContexts
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private MangaContext MangaContext = null!;

    protected override void SetContexts(IServiceScope serviceScope)
    {
        MangaContext = GetContext<MangaContext>(serviceScope);
    }
    
    protected override async Task<BaseWorker[]> DoWorkInternal()
    {
        Log.Info("Cleaning up old connector-data...");
        string[] connectorNames = Tranga.MangaConnectors.Select(c => c.Name).ToArray();
        int deletedChapterIds = await MangaContext.MangaConnectorToChapter.Where(chId => connectorNames.All(n => n != chId.MangaConnectorName)).ExecuteDeleteAsync(CancellationToken);
        Log.InfoFormat("Deleted {0} chapterIds.", deletedChapterIds);
        
        // Manga without Connector get printed to file, to not lose data...
        if (await MangaContext.MangaConnectorToManga.Include(id => id.Obj) .Where(mcId => connectorNames.All(name => name != mcId.MangaConnectorName)).ToListAsync() is { Count: > 0 } list)
        {
            string filePath = Path.Join(TrangaSettings.WorkingDirectory, $"deletedManga-{DateTime.UtcNow.Ticks}.txt");
            Log.DebugFormat("Writing deleted manga to {0}.", filePath);
            await File.WriteAllLinesAsync(filePath, list.Select(id => string.Join('-', id.MangaConnectorName, id.IdOnConnectorSite, id.Obj.Name, id.WebsiteUrl)), CancellationToken);
        }
        int deletedMangaIds = await MangaContext.MangaConnectorToManga.Where(mcId => connectorNames.All(name => name != mcId.MangaConnectorName)).ExecuteDeleteAsync(CancellationToken);
        Log.InfoFormat("Deleted {0} mangaIds.", deletedMangaIds);
        
        
        if(await MangaContext.Sync(CancellationToken, GetType(), "Cleanup done") is { success: false } e)
            Log.ErrorFormat("Failed to save database changes: {0}", e.exceptionMessage);
        
        return [];
    }
}