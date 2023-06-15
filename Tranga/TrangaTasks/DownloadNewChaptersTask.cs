using Logging;
using Newtonsoft.Json;

namespace Tranga.TrangaTasks;

public class DownloadNewChaptersTask : TrangaTask
{
    public string connectorName { get; }
    public Publication publication { get; }
    public string language { get; }
    public DownloadNewChaptersTask(Task task, string connectorName, Publication publication, TimeSpan reoccurrence, string language = "en") : base(task, reoccurrence)
    {
        this.connectorName = connectorName;
        this.publication = publication;
        this.language = language;
    }

    protected override void ExecuteTask(TaskManager taskManager, Logger? logger, CancellationToken? cancellationToken = null)
    {
        if (cancellationToken?.IsCancellationRequested??false)
            return;
        Publication pub = publication!;
        Connector connector = taskManager.GetConnector(this.connectorName);
        
        //Check if Publication already has a Folder
        pub.CreatePublicationFolder(taskManager.settings.downloadLocation);
        List<Chapter> newChapters = taskManager.GetNewChaptersList(connector, pub, language!);

        connector.CopyCoverFromCacheToDownloadLocation(pub, taskManager.settings);
        
        pub.SaveSeriesInfoJson(connector.downloadLocation);

        foreach (Chapter newChapter in newChapters)
        {
            DownloadChapterTask newTask = new (Task.DownloadChapter, this.connectorName, pub, newChapter, this.language, this);
            taskManager.AddTask(newTask);
            this.childTasks.Add(newTask);
        }
    }

    public override string ToString()
    {
        return $"{base.ToString()}, {connectorName}, {publication.sortName} {publication.internalId}";
    }
}