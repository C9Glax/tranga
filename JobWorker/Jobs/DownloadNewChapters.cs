using API.Schema;
using API.Schema.Jobs;
using MangaConnector = Tranga.MangaConnectors.MangaConnector;

namespace JobWorker.Jobs;

public class DownloadNewChapters(DownloadNewChaptersJob data) : Job<DownloadNewChaptersJob>(data)
{
    
    private const string AddChaptersEndpoint = "v2/Manga/{0}";
    protected override IEnumerable<Job> ExecuteReturnSubTasksInternal(DownloadNewChaptersJob data)
    {
        Manga manga = data.Manga;
        MangaConnector mangaConnector = GetConnector(manga);
        Chapter[] chapters = mangaConnector.GetChapters(manga);
        
        Monitor.MakePutRequestApi(string.Format(AddChaptersEndpoint, manga.MangaId), chapters, out object? _);
        
        return chapters.Select(c => new DownloadSingleChapterJob(c.ChapterId));
    }
}