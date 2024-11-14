using API.Schema;
using API.Schema.Jobs;
using MangaConnector = Tranga.MangaConnectors.MangaConnector;

namespace JobWorker.Jobs;

public class DownloadNewChapters : Job<Manga, Chapter[]>
{
    protected override (IEnumerable<Job>, Chapter[]) ExecuteReturnSubTasksInternal(Manga manga)
    {
        MangaConnector mangaConnector = GetConnector(manga);
        Chapter[] chapters = mangaConnector.GetChapters(manga);
        return (chapters.Select(c => new DownloadSingleChapterJob(c.ChapterId)), chapters);
    }
}