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
        List<Chapter> newChapters = GetNewChaptersList(connector, pub, language!, ref taskManager.chapterCollection);

        connector.CopyCoverFromCacheToDownloadLocation(pub, taskManager.settings);
        
        pub.SaveSeriesInfoJson(connector.downloadLocation);

        foreach (Chapter newChapter in newChapters)
        {
            DownloadChapterTask newTask = new (Task.DownloadChapter, this.connectorName, pub, newChapter, this.language, this);
            taskManager.AddTask(newTask);
            this.childTasks.Add(newTask);
        }
    }
    
    /// <summary>
    /// Updates the available Chapters of a Publication
    /// </summary>
    /// <param name="connector">Connector to use</param>
    /// <param name="publication">Publication to check</param>
    /// <param name="language">Language to receive chapters for</param>
    /// <param name="chapterCollection"></param>
    /// <returns>List of Chapters that were previously not in collection</returns>
    private static List<Chapter> GetNewChaptersList(Connector connector, Publication publication, string language, ref Dictionary<Publication, List<Chapter>> chapterCollection)
    {
        List<Chapter> newChaptersList = new();
        chapterCollection.TryAdd(publication, newChaptersList); //To ensure publication is actually in collection
        
        Chapter[] newChapters = connector.GetChapters(publication, language);
        newChaptersList = newChapters.Where(nChapter => !connector.CheckChapterIsDownloaded(publication, nChapter)).ToList();
        
        return newChaptersList;
    }

    public override string ToString()
    {
        return $"{base.ToString()}, {connectorName}, {publication.sortName} {publication.internalId}";
    }
}