using System.Net;
using Logging;

namespace Tranga.TrangaTasks;

public class DownloadChapterTask : TrangaTask
{
    public string connectorName { get; }
    public Publication publication { get; }
    public string language { get; }
    public Chapter chapter { get; }

    private double _dctProgress = 0;

    public DownloadChapterTask(string connectorName, Publication publication, Chapter chapter, string language = "en", MonitorPublicationTask? parentTask = null) : base(Task.DownloadChapter, TimeSpan.Zero, parentTask)
    {
        this.chapter = chapter;
        this.connectorName = connectorName;
        this.publication = publication;
        this.language = language;
    }

    protected override HttpStatusCode ExecuteTask(TaskManager taskManager, Logger? logger, CancellationToken? cancellationToken = null)
    {
        if (cancellationToken?.IsCancellationRequested ?? false)
            return HttpStatusCode.RequestTimeout;
        Connector connector = taskManager.GetConnector(this.connectorName);
        connector.CopyCoverFromCacheToDownloadLocation(this.publication, taskManager.settings);
        HttpStatusCode downloadSuccess = connector.DownloadChapter(this.publication, this.chapter, this, cancellationToken);
        return downloadSuccess;
    }

    public override TrangaTask Clone()
    {
        return new DownloadChapterTask(this.connectorName, this.publication, this.chapter,
            this.language, (MonitorPublicationTask?)this.parentTask);
    }

    protected override double GetProgress()
    {
        return _dctProgress;
    }

    internal void IncrementProgress(double amount)
    {
        this._dctProgress += amount;
        this.lastChange = DateTime.Now;
        if(this.parentTask is not null)
            this.parentTask.lastChange = DateTime.Now;
    }

    public override string ToString()
    {
        return $"{base.ToString()}, {connectorName}, {publication.sortName} {publication.internalId}, Vol.{chapter.volumeNumber} Ch.{chapter.chapterNumber}";
    }
}