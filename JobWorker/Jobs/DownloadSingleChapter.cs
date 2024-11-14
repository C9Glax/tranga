using API.Schema;
using API.Schema.Jobs;
using MangaConnector = Tranga.MangaConnectors.MangaConnector;

namespace JobWorker.Jobs;

public class DownloadSingleChapter : Job<Chapter, object?>
{
    protected override (IEnumerable<Job>, object?) ExecuteReturnSubTasksInternal(Chapter chapter)
    {
        MangaConnector mangaConnector = GetConnector(chapter);
        if (mangaConnector.DownloadChapterImages(chapter, out string? downloadPath) && downloadPath is not null)
        {
            ProcessImagesJob pi = new(downloadPath, false, 100); //TODO parameters
            CreateComicInfoXmlJob cx = new(chapter.ChapterId);
            
            CreateArchiveJob ca = new(downloadPath, chapter.ChapterId, null, [pi.JobId, cx.JobId]);
            return (new Job[]
            {
                pi,
                cx,
                ca
            }, null);
        }
        return (Array.Empty<Job>(), null);
    }
}