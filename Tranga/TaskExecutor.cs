namespace Tranga;

/// <summary>
/// Executes TrangaTasks
/// Based on the TrangaTask.Task a method is called.
/// The chapterCollection is updated with new Publications/Chapters.
/// </summary>
public static class TaskExecutor
{
    
    /// <summary>
    /// Executes TrangaTask.
    /// </summary>
    /// <param name="connectors">List of all available Connectors</param>
    /// <param name="trangaTask">Task to execute</param>
    /// <param name="chapterCollection">Current chapterCollection to update</param>
    /// <exception cref="ArgumentException">Is thrown when there is no Connector available with the name of the TrangaTask.connectorName</exception>
    public static void Execute(Connector[] connectors, TrangaTask trangaTask, Dictionary<Publication, List<Chapter>> chapterCollection)
    {
        //Get Connector from list of available Connectors and the required Connector of the TrangaTask
        Connector? connector = connectors.FirstOrDefault(c => c.name == trangaTask.connectorName);
        if (connector is null)
            throw new ArgumentException($"Connector {trangaTask.connectorName} is not a known connector.");

        if (trangaTask.isBeingExecuted)
            return;
        trangaTask.isBeingExecuted = true;
        trangaTask.lastExecuted = DateTime.Now;
        
        //Call appropriate Method based on TrangaTask.Task
        switch (trangaTask.task)
        {
            case TrangaTask.Task.DownloadNewChapters:
                DownloadNewChapters(connector, (Publication)trangaTask.publication!, trangaTask.language, chapterCollection);
                break;
            case TrangaTask.Task.UpdateChapters:
                UpdateChapters(connector, (Publication)trangaTask.publication!, trangaTask.language, chapterCollection);
                break;
            case TrangaTask.Task.UpdatePublications:
                UpdatePublications(connector, chapterCollection);
                break;
        }

        trangaTask.isBeingExecuted = false;
    }

    /// <summary>
    /// Updates the available Publications from a Connector (all of them)
    /// </summary>
    /// <param name="connector">Connector to receive Publications from</param>
    /// <param name="chapterCollection"></param>
    private static void UpdatePublications(Connector connector, Dictionary<Publication, List<Chapter>> chapterCollection)
    {
        Publication[] publications = connector.GetPublications();
        foreach (Publication publication in publications)
            chapterCollection.TryAdd(publication, new List<Chapter>());
    }

    /// <summary>
    /// Checks for new Chapters and Downloads new ones.
    /// If no Chapters had been downloaded previously, download also cover and create series.json
    /// </summary>
    /// <param name="connector">Connector to use</param>
    /// <param name="publication">Publication to check</param>
    /// <param name="language">Language to receive chapters for</param>
    /// <param name="chapterCollection"></param>
    private static void DownloadNewChapters(Connector connector, Publication publication, string language, Dictionary<Publication, List<Chapter>> chapterCollection)
    {
        List<Chapter> newChapters = UpdateChapters(connector, publication, language, chapterCollection);
        connector.DownloadCover(publication);
        
        //Check if Publication already has a Folder and a series.json
        string publicationFolder = Path.Join(connector.downloadLocation, publication.folderName);
        if(!Directory.Exists(publicationFolder))
            Directory.CreateDirectory(publicationFolder);
        
        string seriesInfoPath = Path.Join(publicationFolder, "series.json");
        if(!File.Exists(seriesInfoPath))
            File.WriteAllText(seriesInfoPath,publication.GetSeriesInfoJson());

        foreach(Chapter newChapter in newChapters)
            connector.DownloadChapter(publication, newChapter);
    }

    /// <summary>
    /// Updates the available Chapters of a Publication
    /// </summary>
    /// <param name="connector">Connector to use</param>
    /// <param name="publication">Publication to check</param>
    /// <param name="language">Language to receive chapters for</param>
    /// <param name="chapterCollection"></param>
    /// <returns>List of Chapters that were previously not in collection</returns>
    private static List<Chapter> UpdateChapters(Connector connector, Publication publication, string language, Dictionary<Publication, List<Chapter>> chapterCollection)
    {
        List<Chapter> newChaptersList = new();
        if (!chapterCollection.ContainsKey(publication))
            return newChaptersList;
        
        List<Chapter> currentChapters = chapterCollection[publication];
        Chapter[] newChapters = connector.GetChapters(publication, language);
        
        newChaptersList = newChapters.ToList()
            .ExceptBy(currentChapters.Select(cChapter => cChapter.url), nChapter => nChapter.url).ToList();
        return newChaptersList;
    }
}