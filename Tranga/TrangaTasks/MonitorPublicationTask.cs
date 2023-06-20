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

    protected override bool ExecuteTask(TaskManager taskManager, Logger? logger, CancellationToken? cancellationToken = null)
    {
        if (cancellationToken?.IsCancellationRequested??false)
            return false;
        Connector connector = taskManager.GetConnector(this.connectorName);
        
        //Check if Publication already has a Folder
        publication.CreatePublicationFolder(taskManager.settings.downloadLocation);
        List<Chapter> newChapters = taskManager.GetNewChaptersList(connector, publication, language);

        connector.CopyCoverFromCacheToDownloadLocation(publication, taskManager.settings);
        
        publication.SaveSeriesInfoJson(connector.downloadLocation);

        foreach (Chapter newChapter in newChapters)
        {
            DownloadChapterTask newTask = new (this.connectorName, publication, newChapter, this.language, this);
            this.childTasks.Add(newTask);
            newTask.state = ExecutionState.Enqueued;
            taskManager.AddTask(newTask);
        }

        return true;
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