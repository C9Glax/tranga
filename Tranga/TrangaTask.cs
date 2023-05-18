using System.Text.Json.Serialization;

namespace Tranga;

public class TrangaTask
{
    [JsonInclude]public TimeSpan reoccurrence { get; }
    [JsonInclude]public DateTime lastExecuted { get; private set; }
    [JsonIgnore] private Connector connector { get; }
    [JsonInclude] public string connectorName;
    [JsonInclude]public AvailableTasks task { get; }
    public enum AvailableTasks
    {
        DownloadNewChapters,
        UpdateChapters,
        UpdatePublications
    };
    [JsonIgnore]public Publication? publication { get; }
    [JsonInclude]public string? publicationIdentifier;
    [JsonInclude]public string language { get; }


    public TrangaTask(Connector connector, AvailableTasks task, TimeSpan reoccurrence, Publication? publication = null, string language = "en")
    {
        this.connector = connector;
        this.connectorName = connector.name;
        this.task = task;
        this.lastExecuted = DateTime.Now.Subtract(reoccurrence);
        this.reoccurrence = reoccurrence;
        this.publication = publication;
        this.publicationIdentifier = publication?.downloadUrl;
        this.language = language;
        if (publication is null && task is AvailableTasks.UpdateChapters or AvailableTasks.DownloadNewChapters)
        {
            if (publication is null)
                throw new ArgumentException(
                    "If task is updateChapters or downloadNewChapters, Argument publication can not be null!");
        }
    }

    public void Execute(ref Dictionary<Publication, Chapter[]> chapterCollection)
    {
        switch (this.task)
        {
            case AvailableTasks.UpdateChapters:
                UpdateChapters(ref chapterCollection);
                break;
            case AvailableTasks.UpdatePublications:
                UpdatePublications(ref chapterCollection);
                break;
            case AvailableTasks.DownloadNewChapters:
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