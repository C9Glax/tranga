using System.Net;
using Tranga.Connectors;
using Tranga.NotificationManagers;
using Tranga.LibraryManagers;

namespace Tranga.TrangaTasks;

public class DownloadChapterTask : TrangaTask
{
    public string connectorName { get; }
    public Publication publication { get; }
    // ReSharper disable once MemberCanBePrivate.Global
    public string language { get; }
    public Chapter chapter { get; }

    private double _dctProgress;

    public DownloadChapterTask(string connectorName, Publication publication, Chapter chapter, string language = "en", MonitorPublicationTask? parentTask = null) : base(Task.DownloadChapter, TimeSpan.Zero, parentTask)
    {
        this.chapter = chapter;
        this.connectorName = connectorName;
        this.publication = publication;
        this.language = language;
    }

    protected override HttpStatusCode ExecuteTask(TaskManager taskManager, CancellationToken? cancellationToken = null)
    {
        if (cancellationToken?.IsCancellationRequested ?? false)
            return HttpStatusCode.RequestTimeout;
        Connector connector = taskManager.GetConnector(this.connectorName);
        connector.CopyCoverFromCacheToDownloadLocation(this.publication);
        HttpStatusCode downloadSuccess = connector.DownloadChapter(this.publication, this.chapter, this, cancellationToken);
        if ((int)downloadSuccess >= 200 && (int)downloadSuccess < 300)
        {
            foreach(NotificationManager nm in taskManager.commonObjects.notificationManagers)
                nm.SendNotification("Chapter downloaded", $"{this.publication.sortName} {this.chapter.chapterNumber} {this.chapter.name}");

            foreach (LibraryManager lm in taskManager.commonObjects.libraryManagers)
                lm.UpdateLibrary();
        }
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