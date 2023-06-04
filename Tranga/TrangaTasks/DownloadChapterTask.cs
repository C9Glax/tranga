using Logging;

namespace Tranga.TrangaTasks;

public class DownloadChapterTask : TrangaTask
{
    public string connectorName { get; }
    public Publication publication { get; }
    public string language { get; }
    public Chapter chapter { get; }
    public DownloadChapterTask(Task task, string connectorName, Publication publication, Chapter chapter, string language = "en") : base(task, TimeSpan.Zero)
    {
        this.chapter = chapter;
        this.connectorName = connectorName;
        this.publication = publication;
        this.language = language;
    }

    protected override void ExecuteTask(TaskManager taskManager, Logger? logger)
    {
        Publication pub = (Publication)this.publication!;
        Connector connector = taskManager.GetConnector(this.connectorName);
        connector.DownloadChapter(pub, this.chapter, this);
        taskManager.DeleteTask(this);
    }
    
    public override string ToString()
    {
        return $"{base.ToString()}, {connectorName}, {publication.sortName} {publication.internalId}, Vol.{chapter.volumeNumber} Ch.{chapter.chapterNumber}";
    }
}