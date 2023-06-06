using Logging;
using Newtonsoft.Json;

namespace Tranga.TrangaTasks;

public class DownloadChapterTask : TrangaTask
{
    public string connectorName { get; }
    public Publication publication { get; }
    public string language { get; }
    public Chapter chapter { get; }
    [JsonIgnore]private DownloadNewChaptersTask? parentTask { get; init; }
    
    public DownloadChapterTask(Task task, string connectorName, Publication publication, Chapter chapter, string language = "en", DownloadNewChaptersTask? parentTask = null) : base(task, TimeSpan.Zero)
    {
        this.chapter = chapter;
        this.connectorName = connectorName;
        this.publication = publication;
        this.language = language;
        this.parentTask = parentTask;
    }

    protected override void ExecuteTask(TaskManager taskManager, Logger? logger)
    {
        Publication pub = (Publication)this.publication!;
        Connector connector = taskManager.GetConnector(this.connectorName);
        connector.DownloadChapter(pub, this.chapter, this);
        taskManager.DeleteTask(this);
    }
    
    public new float IncrementProgress(float amount)
    {
        this.progress += amount;
        parentTask?.IncrementProgress(amount);
        return this.progress;
    }

    public override string ToString()
    {
        return $"{base.ToString()}, {connectorName}, {publication.sortName} {publication.internalId}, Vol.{chapter.volumeNumber} Ch.{chapter.chapterNumber}";
    }
}