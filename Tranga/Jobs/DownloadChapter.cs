using Tranga.MangaConnectors;

namespace Tranga.Jobs;

public class DownloadChapter : Job
{
    public Chapter chapter { get; init; }
    
    public DownloadChapter(GlobalBase clone, MangaConnector connector, Chapter chapter) : base(clone, connector)
    {
        this.chapter = chapter;
    }

    protected override IEnumerable<Job> ExecuteReturnSubTasksInternal()
    {
        Task downloadTask = new(delegate
        {
            mangaConnector.DownloadChapter(chapter, this.progressToken);
            UpdateLibraries();
            SendNotifications("Chapter downloaded", $"{chapter.parentPublication.sortName} - {chapter.chapterNumber}");
        });
        downloadTask.Start();
        return Array.Empty<Job>();
    }
}