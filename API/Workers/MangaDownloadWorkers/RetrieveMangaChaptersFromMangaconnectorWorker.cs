using API.MangaConnectors;
using API.Schema.MangaContext;
using Microsoft.EntityFrameworkCore;

namespace API.Workers;

public class RetrieveMangaChaptersFromMangaconnectorWorker(MangaConnectorId<Manga> mcId, string language, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<MangaContext>(dependsOn)
{
    internal readonly string MangaConnectorIdId = mcId.Key;
    protected override async Task<BaseWorker[]> DoWorkInternal()
    {
        if (await DbContext.MangaConnectorToManga.FirstOrDefaultAsync(c => c.Key == MangaConnectorIdId) is not { } mangaConnectorId)
            return []; //TODO Exception?
        if (!Tranga.TryGetMangaConnector(mangaConnectorId.MangaConnectorName, out MangaConnector? mangaConnector))
            return []; //TODO Exception?
        
        await DbContext.Entry(mangaConnectorId).Navigation(nameof(MangaConnectorId<Manga>.Obj)).LoadAsync(CancellationTokenSource.Token);
        Manga manga = mangaConnectorId.Obj;
        await DbContext.Entry(manga).Collection(m => m.Chapters).LoadAsync(CancellationTokenSource.Token);
        
        // This gets all chapters that are not downloaded
        (Chapter, MangaConnectorId<Chapter>)[] allChapters =
            mangaConnector.GetChapters(mangaConnectorId, language).DistinctBy(c => c.Item1.Key).ToArray();

        int addedChapters = 0;
        foreach ((Chapter chapter, MangaConnectorId<Chapter> mcId) newChapter in allChapters)
        {
            if (Tranga.AddChapterToContext(newChapter, DbContext, out Chapter? addedChapter, CancellationTokenSource.Token) == false)
                continue;
            manga.Chapters.Add(addedChapter);
        }
        Log.Info($"{manga.Chapters.Count} existing + {addedChapters} new chapters.");

        await DbContext.Sync(CancellationTokenSource.Token);

        return [];
    }

    public override string ToString() => $"{base.ToString()} {MangaConnectorIdId}";
}