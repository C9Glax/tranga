using Logging;

namespace Tranga.TrangaTasks;

public class DownloadNewChaptersTask : TrangaTask
{
    public DownloadNewChaptersTask(Task task, string connectorName, Publication publication, TimeSpan reoccurrence, string language = "en") : base(task, connectorName, publication, reoccurrence, language)
    {
    }

    protected override void ExecuteTask(TaskManager taskManager, Logger? logger)
    {
        Publication pub = (Publication)this.publication!;
        Connector connector = taskManager.GetConnector(this.connectorName);

        //Check if Publication already has a Folder
        string publicationFolder = Path.Join(connector.downloadLocation, pub.folderName);
        if(!Directory.Exists(publicationFolder))
            Directory.CreateDirectory(publicationFolder);
        List<Chapter> newChapters = UpdateChapters(connector, pub, language!, ref taskManager.chapterCollection);

        connector.CopyCoverFromCacheToDownloadLocation(pub, taskManager.settings);
        
        pub.SaveSeriesInfoJson(connector.downloadLocation);

        foreach(Chapter newChapter in newChapters)
            connector.DownloadChapter(pub, newChapter);
    }
    
    /// <summary>
    /// Updates the available Chapters of a Publication
    /// </summary>
    /// <param name="connector">Connector to use</param>
    /// <param name="publication">Publication to check</param>
    /// <param name="language">Language to receive chapters for</param>
    /// <param name="chapterCollection"></param>
    /// <returns>List of Chapters that were previously not in collection</returns>
    private static List<Chapter> UpdateChapters(Connector connector, Publication publication, string language, ref Dictionary<Publication, List<Chapter>> chapterCollection)
    {
        List<Chapter> newChaptersList = new();
        chapterCollection.TryAdd(publication, newChaptersList); //To ensure publication is actually in collection
        
        Chapter[] newChapters = connector.GetChapters(publication, language);
        newChaptersList = newChapters.Where(nChapter => !connector.CheckChapterIsDownloaded(publication, nChapter)).ToList();
        
        return newChaptersList;
    }
}