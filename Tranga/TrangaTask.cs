namespace Tranga;

public class TrangaTask
{
    public TimeSpan reoccurrence { get; }
    public DateTime lastExecuted { get; private set; }
    public Connector connector { get; }
    public availableTasks availableTaskToExecute { get; }
    public enum availableTasks
    {
        downloadNewChapters,
        updateChapters,
        updatePublications
    };
    public Publication? publication { get; }
    public string language { get; }


    public TrangaTask(Connector connector, availableTasks availableTask, TimeSpan reoccurrence, Publication? publication = null, string language = "en")
    {
        this.connector = connector;
        this.availableTaskToExecute = availableTask;
        this.lastExecuted = DateTime.Now.Subtract(reoccurrence);
        this.reoccurrence = reoccurrence;
        this.publication = publication;
        this.language = language;
        if (publication is null && availableTask is availableTasks.updateChapters or availableTasks.downloadNewChapters)
        {
            if (publication is null)
                throw new ArgumentException(
                    "If task is updateChapters or downloadNewChapters, Argument publication can not be null!");
        }
    }

    public void Execute(ref Dictionary<Publication, Chapter[]> chapterCollection)
    {
        switch (this.availableTaskToExecute)
        {
            case availableTasks.updateChapters:
                UpdateChapters(ref chapterCollection);
                break;
            case availableTasks.updatePublications:
                UpdatePublications(ref chapterCollection);
                break;
            case availableTasks.downloadNewChapters:
                DownloadNewChapters(UpdateChapters(ref chapterCollection));
                break;
        }

        this.lastExecuted = DateTime.Now;
    }

    private Chapter[] UpdateChapters(ref Dictionary<Publication, Chapter[]> chapterCollection)
    {
        Publication pPublication = (Publication)this.publication!;
        Chapter[] presentChapters = chapterCollection[pPublication];
        Chapter[] allChapters = connector.GetChapters(pPublication);
        chapterCollection[pPublication] = allChapters;

        Dictionary<string, Chapter> pChapter = presentChapters.ToDictionary(chapter => chapter.url, chapter => chapter);
        Dictionary<string, Chapter> aChapter = allChapters.ToDictionary(chapter => chapter.url, chapter => chapter);
        return aChapter.Except(pChapter).ToDictionary(pair => pair.Key, pair => pair.Value).Values.ToArray();
    }

    private void UpdatePublications(ref Dictionary<Publication, Chapter[]> chapterCollection)
    {
        Publication[] allPublications = connector.GetPublications();
        foreach(Publication publication in allPublications)
            chapterCollection.TryAdd(publication, Array.Empty<Chapter>());
    }

    private void DownloadNewChapters(Chapter[] newChapters)
    {
        Publication pPublication = (Publication)this.publication!;
        foreach(Chapter chapter in newChapters)
            connector.DownloadChapter(pPublication, chapter);
    }
}