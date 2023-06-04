using Logging;

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

    protected override void ExecuteTask(TaskManager taskManager, Logger? logger)
    {
        Publication pub = publication!;
        Connector connector = taskManager.GetConnector(this.connectorName);
        this.progress = 0.1f;
        
        //Check if Publication already has a Folder
        pub.CreatePublicationFolder(taskManager.settings.downloadLocation);
        this.progress = 0.2f;
        List<Chapter> newChapters = GetNewChaptersList(connector, pub, language!, ref taskManager.chapterCollection);
        this.progress = 0.6f;

        connector.CopyCoverFromCacheToDownloadLocation(pub, taskManager.settings);
        this.progress = 0.7f;
        
        pub.SaveSeriesInfoJson(connector.downloadLocation);
        this.progress = 0.8f;

        foreach (Chapter newChapter in newChapters)
            taskManager.AddTask(new DownloadChapterTask(Task.DownloadChapter, this.connectorName!, pub, newChapter, this.language));
        this.progress = 1f;
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