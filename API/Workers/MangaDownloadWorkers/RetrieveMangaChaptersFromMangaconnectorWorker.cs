using API.Schema.MangaContext;
using API.Schema.MangaContext.MangaConnectors;

namespace API.Workers;

public class RetrieveMangaChaptersFromMangaconnectorWorker(MangaConnectorId<Manga> mcId, string language, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<MangaContext>(dependsOn)
{
    public MangaConnectorId<Manga> MangaConnectorId { get; init; } = mcId;
    protected override BaseWorker[] DoWorkInternal()
    {
        MangaConnector mangaConnector = MangaConnectorId.MangaConnector;
        Manga manga = MangaConnectorId.Obj;
        // This gets all chapters that are not downloaded
        (Chapter, MangaConnectorId<Chapter>)[] allChapters =
            mangaConnector.GetChapters(MangaConnectorId, language).DistinctBy(c => c.Item1.Key).ToArray();

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
}