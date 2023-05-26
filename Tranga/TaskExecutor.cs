using Logging;

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
    /// <param name="taskManager">Parent</param>
    /// <param name="trangaTask">Task to execute</param>
    /// <param name="chapterCollection">Current chapterCollection to update</param>
    /// <param name="logger"></param>
    /// <exception cref="ArgumentException">Is thrown when there is no Connector available with the name of the TrangaTask.connectorName</exception>
    public static void Execute(TaskManager taskManager, TrangaTask trangaTask, Logger? logger)
    {
        //Only execute task if it is not already being executed.
        if (trangaTask.state == TrangaTask.ExecutionState.Running)
        {
            logger?.WriteLine("TaskExecutor", $"Task already running {trangaTask}");
            return;
        }
        trangaTask.state = TrangaTask.ExecutionState.Running;
        logger?.WriteLine("TaskExecutor", $"Starting Task {trangaTask}");
        
        //Connector is not needed for all tasks
        Connector? connector = null;
        if (trangaTask.task != TrangaTask.Task.UpdateKomgaLibrary)
            connector = taskManager.GetConnector(trangaTask.connectorName!);

        //Call appropriate Method based on TrangaTask.Task
        switch (trangaTask.task)
        {
            case TrangaTask.Task.DownloadNewChapters:
                DownloadNewChapters(connector!, (Publication)trangaTask.publication!, trangaTask.language, ref taskManager._chapterCollection, taskManager.settings);
                break;
            case TrangaTask.Task.UpdateChapters:
                UpdateChapters(connector!, (Publication)trangaTask.publication!, trangaTask.language, ref taskManager._chapterCollection);
                break;
            case TrangaTask.Task.UpdatePublications:
                UpdatePublications(connector!, ref taskManager._chapterCollection);
                break;
            case TrangaTask.Task.UpdateKomgaLibrary:
                UpdateKomgaLibrary(taskManager);
                break;
        }
        
        logger?.WriteLine("TaskExecutor", $"Task finished! {trangaTask}");
        trangaTask.lastExecuted = DateTime.Now;
        trangaTask.state = TrangaTask.ExecutionState.Waiting;
    }

    /// <summary>
    /// Updates all Komga-Libraries
    /// </summary>
    /// <param name="taskManager">Parent</param>
    private static void UpdateKomgaLibrary(TaskManager taskManager)
    {
        if (taskManager.komga is null)
            return;
        Komga komga = taskManager.komga;

        Komga.KomgaLibrary[] allLibraries = komga.GetLibraries();
        foreach (Komga.KomgaLibrary lib in allLibraries)
            komga.UpdateLibrary(lib.id);
    }

    /// <summary>
    /// Updates the available Publications from a Connector (all of them)
    /// </summary>
    /// <param name="connector">Connector to receive Publications from</param>
    /// <param name="chapterCollection"></param>
    private static void UpdatePublications(Connector connector, ref Dictionary<Publication, List<Chapter>> chapterCollection)
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
    private static void DownloadNewChapters(Connector connector, Publication publication, string language, ref Dictionary<Publication, List<Chapter>> chapterCollection, TrangaSettings settings)
    {
        //Check if Publication already has a Folder
        string publicationFolder = Path.Join(connector.downloadLocation, publication.folderName);
        if(!Directory.Exists(publicationFolder))
            Directory.CreateDirectory(publicationFolder);
        List<Chapter> newChapters = UpdateChapters(connector, publication, language, ref chapterCollection);

        connector.CloneCoverFromCache(publication, settings);
        
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
    private static List<Chapter> UpdateChapters(Connector connector, Publication publication, string language, ref Dictionary<Publication, List<Chapter>> chapterCollection)
    {
        List<Chapter> newChaptersList = new();
        chapterCollection.TryAdd(publication, newChaptersList); //To ensure publication is actually in collection
        
        Chapter[] newChapters = connector.GetChapters(publication, language);
        newChaptersList = newChapters.Where(nChapter => !connector.ChapterIsDownloaded(publication, nChapter)).ToList();
        
        return newChaptersList;
    }
}