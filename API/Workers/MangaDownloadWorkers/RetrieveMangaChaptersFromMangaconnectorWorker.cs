using API.MangaConnectors;
using API.Schema.MangaContext;

namespace API.Workers;

public class RetrieveMangaChaptersFromMangaconnectorWorker(MangaConnectorId<Manga> mcId, string language, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<MangaContext>(dependsOn)
{
    internal readonly string MangaConnectorIdId = mcId.Key;
    protected override BaseWorker[] DoWorkInternal()
    {
        if (DbContext.MangaConnectorToManga.Find(MangaConnectorIdId) is not { } mangaConnectorId)
            return []; //TODO Exception?
        if (!Tranga.TryGetMangaConnector(mangaConnectorId.MangaConnectorName, out MangaConnector? mangaConnector))
            return []; //TODO Exception?
        
        DbContext.Entry(mangaConnectorId).Navigation(nameof(MangaConnectorId<Manga>.Obj)).Load();
        Manga manga = mangaConnectorId.Obj;
        DbContext.Entry(manga).Collection(m => m.Chapters).Load();
        
        // This gets all chapters that are not downloaded
        (Chapter, MangaConnectorId<Chapter>)[] allChapters =
            mangaConnector.GetChapters(mangaConnectorId, language).DistinctBy(c => c.Item1.Key).ToArray();

        int addedChapters = 0;
        foreach ((Chapter chapter, MangaConnectorId<Chapter> mcId) newChapter in allChapters)
        {
            if (Tranga.AddChapterToContext(newChapter, DbContext, out Chapter? addedChapter) == false)
                continue;
            manga.Chapters.Add(addedChapter);
        }
        Log.Info($"{manga.Chapters.Count} existing + {addedChapters} new chapters.");

        DbContext.Sync();

        return [];
    }

    public override string ToString() => $"{base.ToString()} {MangaConnectorIdId}";
}