using API.MangaConnectors;
using API.Schema.MangaContext;
using Microsoft.EntityFrameworkCore;

namespace API.Workers;

/// <summary>
/// Retrieves the metadata of available chapters on the Mangaconnector
/// </summary>
/// <param name="mcId"></param>
/// <param name="language"></param>
/// <param name="dependsOn"></param>
public class RetrieveMangaChaptersFromMangaconnectorWorker(MangaConnectorId<Manga> mcId, string language, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<MangaContext>(dependsOn)
{
    internal readonly string MangaConnectorIdId = mcId.Key;
    protected override async Task<BaseWorker[]> DoWorkInternal()
    {
        Log.Debug($"Getting Chapters for MangaConnectorId {MangaConnectorIdId}...");
        // Getting MangaConnector info
        if (await DbContext.MangaConnectorToManga
                .Include(id => id.Obj)
                .FirstOrDefaultAsync(c => c.Key == MangaConnectorIdId, CancellationToken) is not { } mangaConnectorId)
        {
            Log.Error("Could not get MangaConnectorId.");
            return []; //TODO Exception?
        }
        if (!Tranga.TryGetMangaConnector(mangaConnectorId.MangaConnectorName, out MangaConnector? mangaConnector))
        {
            Log.Error("Could not get MangaConnector.");
            return []; //TODO Exception?
        }
        Log.Debug($"Getting Chapters for MangaConnectorId {mangaConnectorId}...");
        
        Manga manga = mangaConnectorId.Obj;
        // Load existing Chapters (in database)
        await DbContext.Entry(manga).Collection(m => m.Chapters).LoadAsync(CancellationToken);
        
        // This gets all chapters that are not downloaded
        (Chapter chapter, MangaConnectorId<Chapter> chapterId)[] allChapters =
            mangaConnector.GetChapters(mangaConnectorId, language).DistinctBy(c => c.Item1.Key).ToArray();
        
        int beforeAmount = manga.Chapters.Count;
        Log.Debug($"Got {allChapters.Length} chapters from connector.");
        DbContext.Entry(manga).Collection(m => m.Chapters).CurrentValue = manga.Chapters.UnionBy(allChapters.Select(c => c.chapter), c => c.Key);
        int afterAmount = manga.Chapters.Count;
        
        Log.Debug($"Got {afterAmount} new chapters.");

        if(await DbContext.Sync(CancellationToken) is { success: false } e)
            Log.Error($"Failed to save database changes: {e.exceptionMessage}");

        return [];
    }

    public override string ToString() => $"{base.ToString()} {MangaConnectorIdId}";
}