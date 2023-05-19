namespace Tranga;

public static class TaskExecutor
{
    public static void Execute(Connector[] connectors, TrangaTask trangaTask, Dictionary<Publication, List<Chapter>> chapterCollection)
    {
        Connector? connector = connectors.FirstOrDefault(c => c.name == trangaTask.connectorName);
        if (connector is null)
            throw new ArgumentException($"Connector {trangaTask.connectorName} is not a known connector.");

        if (trangaTask.isBeingExecuted)
            return;
        trangaTask.isBeingExecuted = true;
        trangaTask.lastExecuted = DateTime.Now;
        
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

    private static void UpdatePublications(Connector connector, Dictionary<Publication, List<Chapter>> chapterCollection)
    {
        Publication[] publications = connector.GetPublications();
        foreach (Publication publication in publications)
            chapterCollection.TryAdd(publication, new List<Chapter>());
    }

    private static void DownloadNewChapters(Connector connector, Publication publication, string language, Dictionary<Publication, List<Chapter>> chapterCollection)
    {
        List<Chapter> newChapters = UpdateChapters(connector, publication, language, chapterCollection);
        foreach(Chapter newChapter in newChapters)
            connector.DownloadChapter(publication, newChapter);
        connector.DownloadCover(publication);
        connector.SaveSeriesInfo(publication);
    }

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