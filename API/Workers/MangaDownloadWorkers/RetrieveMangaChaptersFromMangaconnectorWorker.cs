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
                .ThenInclude(m => m.Chapters)
                .ThenInclude(ch => ch.MangaConnectorIds)
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
        
        // Retrieve available Chapters from Connector
        (Chapter chapter, MangaConnectorId<Chapter> chapterId)[] allChapters =
            mangaConnector.GetChapters(mangaConnectorId, language).DistinctBy(c => c.Item1.Key).ToArray();
        Log.Debug($"Got {allChapters.Length} chapters from connector.");
        
        // Filter for new Chapters
        List<(Chapter chapter, MangaConnectorId<Chapter> chapterId)> newChapters = allChapters.Where<(Chapter chapter, MangaConnectorId<Chapter> chapterId)>(ch =>
            manga.Chapters.All(c => c.Key != ch.chapter.Key)).ToList();
        Log.Debug($"Got {newChapters.Count} new chapters.");

        // Add Chapters to Manga
        manga.Chapters = manga.Chapters.Union(newChapters.Select(ch => ch.chapter)).ToList();
        
        // Filter for new ChapterIds
        List<MangaConnectorId<Chapter>> existingChapterIds = manga.Chapters.SelectMany(c => c.MangaConnectorIds).ToList();
        List<MangaConnectorId<Chapter>> newIds = allChapters.Select(ch => ch.chapterId)
            .Where(newCh => !existingChapterIds.Any(existing =>
                existing.MangaConnectorName == newCh.MangaConnectorName &&
                existing.IdOnConnectorSite == newCh.IdOnConnectorSite))
            .ToList();
        // Match tracked entities of Chapters
        foreach (MangaConnectorId<Chapter> newId in newIds)
            newId.Obj = manga.Chapters.First(ch => ch.Key == newId.ObjId);
        Log.Debug($"Got {newIds.Count} new download-Ids.");
        
        // Add new ChapterIds to Database
        DbContext.MangaConnectorToChapter.AddRange(newIds);

        // If Manga is marked for Download from Connector, mark the new Chapters as UseForDownload
        if (mangaConnectorId.UseForDownload)
        {
            foreach ((Chapter _, MangaConnectorId<Chapter> chapterId) in newChapters)
            {
                chapterId.UseForDownload = mangaConnectorId.UseForDownload;
            }
        }

        if(await DbContext.Sync(CancellationToken) is { success: false } e)
            Log.Error($"Failed to save database changes: {e.exceptionMessage}");

        return [];
    }

    public override string ToString() => $"{base.ToString()} {MangaConnectorIdId}";
}