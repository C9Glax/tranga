using API.Schema;
using API.Schema.Jobs;
using MangaConnector = Tranga.MangaConnectors.MangaConnector;

namespace JobWorker.Jobs;

public class UpdateMetadata : Job<Manga, Manga?>
{
    protected override (IEnumerable<Job>, Manga?) ExecuteReturnSubTasksInternal(Manga manga)
    {
        MangaConnector mangaConnector = GetConnector(manga);
        //Retrieve new Metadata
        Manga? retManga = mangaConnector.GetMangaFromManga(manga);
        return (Array.Empty<Job>(), retManga);
    }
}