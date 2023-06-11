using Logging;
using Newtonsoft.Json;

namespace Tranga.TrangaTasks;

public class DownloadChapterTask : TrangaTask
{
    public string connectorName { get; }
    public Publication publication { get; }
    public string language { get; }
    public Chapter chapter { get; }

    public DownloadChapterTask(Task task, string connectorName, Publication publication, Chapter chapter, string language = "en", DownloadNewChaptersTask? parentTask = null) : base(task, TimeSpan.Zero, parentTask)
    {
        this.chapter = chapter;
        this.connectorName = connectorName;
        this.publication = publication;
        this.language = language;
    }

    protected override void ExecuteTask(TaskManager taskManager, Logger? logger, CancellationToken? cancellationToken = null)
    {
        if (cancellationToken?.IsCancellationRequested??false)
            return;
        if(this.parentTask is not null)
            this.parentTask.state = ExecutionState.Running;
        Connector connector = taskManager.GetConnector(this.connectorName);
        connector.DownloadChapter(this.publication, this.chapter, this, cancellationToken);
        if(this.parentTask is not null)
            this.parentTask.state = ExecutionState.Waiting;
        taskManager.DeleteTask(this);
    }

    public override string ToString()
    {
        return $"{base.ToString()}, {connectorName}, {publication.sortName} {publication.internalId}, Vol.{chapter.volumeNumber} Ch.{chapter.chapterNumber}";
    }
}