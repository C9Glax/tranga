using API.Schema.MangaContext;
using API.Schema.MangaContext.MangaConnectors;
using Microsoft.EntityFrameworkCore;

namespace API.Workers;

public class RetrieveMangaChaptersFromMangaconnectorWorker(MangaConnectorId<Manga> mcId, string language, IServiceScope scope, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<MangaContext>(scope, dependsOn)
{
    public MangaConnectorId<Manga> MangaConnectorId { get; init; } = mcId;
    protected override BaseWorker[] DoWorkInternal()
    {
        MangaConnector mangaConnector = MangaConnectorId.MangaConnector;
        Manga manga = MangaConnectorId.Obj;
        // This gets all chapters that are not downloaded
        (Chapter, MangaConnectorId<Chapter>)[] allChapters =
            mangaConnector.GetChapters(MangaConnectorId, language).DistinctBy(c => c.Item1.Key).ToArray();
        (Chapter, MangaConnectorId<Chapter>)[] newChapters = allChapters.Where(chapter =>
            manga.Chapters.Any(ch => chapter.Item1.Key == ch.Key && ch.Downloaded) == false).ToArray();
        Log.Info($"{manga.Chapters.Count} existing + {newChapters.Length} new chapters.");

        try
        {
            foreach ((Chapter chapter, MangaConnectorId<Chapter> mcId) newChapter in newChapters)
            {
                manga.Chapters.Add(newChapter.chapter);
                DbContext.MangaConnectorToChapter.Add(newChapter.mcId);
            }

            DbContext.SaveChanges();
        }
        catch (DbUpdateException e)
        {
            Log.Error(e);
        }

        return [];
    }
}