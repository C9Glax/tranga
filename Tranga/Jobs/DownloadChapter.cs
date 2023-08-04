using Tranga.MangaConnectors;

namespace Tranga.Jobs;

public class DownloadChapter : Job
{
    public Chapter chapter { get; init; }
    
    public DownloadChapter(MangaConnector connector, Chapter chapter) : base(connector)
    {
        this.chapter = chapter;
    }

    protected override IEnumerable<Job> ExecuteReturnSubTasksInternal()
    {
        Task downloadTask = new(delegate
        {
            mangaConnector.DownloadChapter(chapter, this.progressToken);
        });
        downloadTask.Start();
        return Array.Empty<Job>();
    }
}