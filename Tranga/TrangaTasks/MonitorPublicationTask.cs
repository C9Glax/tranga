using System.Net;
using Logging;

namespace Tranga.TrangaTasks;

public class MonitorPublicationTask : TrangaTask
{
    public string connectorName { get; }
    public Publication publication { get; }
    public string language { get; }
    public MonitorPublicationTask(string connectorName, Publication publication, TimeSpan reoccurrence, string language = "en") : base(Task.MonitorPublication, reoccurrence)
    {
        this.connectorName = connectorName;
        this.publication = publication;
        this.language = language;
    }

    protected override HttpStatusCode ExecuteTask(TaskManager taskManager, Logger? logger, CancellationToken? cancellationToken = null)
    {
        if (cancellationToken?.IsCancellationRequested ?? false)
            return HttpStatusCode.RequestTimeout;
        Connector connector = taskManager.GetConnector(this.connectorName);
        
        //Check if Publication already has a Folder
        publication.CreatePublicationFolder(taskManager.settings.downloadLocation);
        List<Chapter> newChapters = connector.GetNewChaptersList(publication, language, ref taskManager.collection);

        connector.CopyCoverFromCacheToDownloadLocation(publication, taskManager.settings);
        
        publication.SaveSeriesInfoJson(taskManager.settings.downloadLocation);

        foreach (Chapter newChapter in newChapters)
        {
            DownloadChapterTask newTask = new (this.connectorName, publication, newChapter, this.language, this);
            this.childTasks.Add(newTask);
            newTask.state = ExecutionState.Enqueued;
            taskManager.AddTask(newTask);
        }

        return HttpStatusCode.OK;
    }

    public override TrangaTask Clone()
    {
        return new MonitorPublicationTask(this.connectorName, this.publication, this.reoccurrence,
            this.language);
    }

    protected override double GetProgress()
    {
        if (this.childTasks.Count > 0)
            return this.childTasks.Sum(ct => ct.progress) / childTasks.Count;
        return 1;
    }

    public override string ToString()
    {
        return $"{base.ToString()}, {connectorName}, {publication.sortName} {publication.internalId}";
    }
}