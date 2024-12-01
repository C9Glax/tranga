using API.Schema;
using API.Schema.Jobs;
using Tranga;
using MangaConnector = Tranga.MangaConnectors.MangaConnector;

namespace JobWorker.Jobs;

public class DownloadSingleChapter(DownloadSingleChapterJob data) : Job<DownloadSingleChapterJob>(data)
{
    protected override IEnumerable<Job> ExecuteReturnSubTasksInternal(DownloadSingleChapterJob data)
    {
        Chapter chapter = data.Chapter;
        MangaConnector mangaConnector = GetConnector(chapter);
        if (mangaConnector.DownloadChapterImages(chapter, out string? downloadPath) && downloadPath is not null)
        {
            ProcessImagesJob pi = new(downloadPath, TrangaSettings.bwImages, TrangaSettings.compression);
            CreateComicInfoXmlJob cx = new(chapter.ChapterId, downloadPath);
            
            CreateArchiveJob ca = new(downloadPath, Path.Join(downloadPath, CreateComicInfoXml.ComicInfoXmlFileName), chapter.ChapterId, null, [pi.JobId, cx.JobId]);
            return
            [
                pi,
                cx,
                ca
            ];
        }
        return [];
    }
}