using API.Schema;
using API.Schema.Jobs;
using Tranga;
using MangaConnector = Tranga.MangaConnectors.MangaConnector;

namespace JobWorker.Jobs;

public class DownloadSingleChapter : Job<Chapter, string?>
{
    protected override (IEnumerable<Job>, string?) ExecuteReturnSubTasksInternal(Chapter chapter, Job[] relatedJobs)
    {
        MangaConnector mangaConnector = GetConnector(chapter);
        if (mangaConnector.DownloadChapterImages(chapter, out string? downloadPath) && downloadPath is not null)
        {
            ProcessImagesJob pi = new(downloadPath, TrangaSettings.bwImages, TrangaSettings.compression);
            CreateComicInfoXmlJob cx = new(chapter.ChapterId);
            
            CreateArchiveJob ca = new(downloadPath, chapter.ChapterId, null, [pi.JobId, cx.JobId]);
            return (new Job[]
            {
                pi,
                cx,
                ca
            }, downloadPath);
        }
        return ([], null);
    }
}