namespace Tranga;

public static class TaskExecutor
{
    public static void Execute(TrangaTask trangaTask, Dictionary<Publication, List<Chapter>> chapterCollection)
    {
        switch (trangaTask.task)
        {
            case TrangaTask.Task.DownloadNewChapters:
                DownloadNewChapters(trangaTask.connector, trangaTask.publication, trangaTask.language, chapterCollection);
                break;
            case TrangaTask.Task.UpdateChapters:
                UpdateChapters(trangaTask.connector, trangaTask.publication, trangaTask.language, chapterCollection);
                break;
            case TrangaTask.Task.UpdatePublications:
                UpdatePublications(trangaTask.connector, chapterCollection);
                break;
        }
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