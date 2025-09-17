using System.Text;
using API.Schema.MangaContext;
using Microsoft.EntityFrameworkCore;

namespace API.Workers.PeriodicWorkers.MaintenanceWorkers;

public class CleanupMangaconnectorIdsWithoutConnector : BaseWorkerWithContext<MangaContext>
{
    protected override async Task<BaseWorker[]> DoWorkInternal()
    {
        Log.Info("Cleaning up old connector-data");
        string[] connectorNames = Tranga.MangaConnectors.Select(c => c.Name).ToArray();
        int deletedChapterIds = await DbContext.MangaConnectorToChapter.Where(chId => connectorNames.All(n => n != chId.MangaConnectorName)).ExecuteDeleteAsync(CancellationToken);
        Log.Info($"Deleted {deletedChapterIds} chapterIds.");
        
        // Manga without Connector get printed to file, to not lose data...
        if (await DbContext.MangaConnectorToManga.Include(id => id.Obj)
                .Where(mcId => connectorNames.Any(name => name == mcId.MangaConnectorName)).ToListAsync() is
            { Count: > 0 } list)
        {
            string filePath = Path.Join(TrangaSettings.WorkingDirectory, $"deletedManga-{DateTime.UtcNow.Ticks}.txt");
            Log.Debug($"Writing deleted manga to {filePath}.");
            await File.WriteAllLinesAsync(filePath, list.Select(id => string.Join('-', id.MangaConnectorName, id.IdOnConnectorSite, id.Obj.Name, id.WebsiteUrl)), CancellationToken);
        }
        int deletedMangaIds = await DbContext.MangaConnectorToManga.Where(mcId => connectorNames.All(name => name != mcId.MangaConnectorName)).ExecuteDeleteAsync(CancellationToken);
        Log.Info($"Deleted {deletedMangaIds} mangaIds.");
        return [];
    }
}