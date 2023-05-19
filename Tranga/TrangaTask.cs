using System.Text.Json.Serialization;

namespace Tranga;

public struct TrangaTask
{
    [JsonInclude]public TimeSpan reoccurrence { get; }
    [JsonInclude]public DateTime lastExecuted { get; set; }
    [JsonInclude]public Connector connector { get; }
    [JsonInclude]public Task task { get; }
    [JsonInclude]public Publication publication { get; }
    [JsonInclude]public string language { get; }

    public TrangaTask(Connector connector, Task task, Publication publication, TimeSpan reoccurrence, string language = "")
    {
        this.reoccurrence = reoccurrence;
        this.lastExecuted = DateTime.Now.Subtract(reoccurrence);
        this.connector = connector;
        this.task = task;
        this.publication = publication;
        this.language = language;
    }

    public bool ShouldExecute(bool willBeExecuted)
    {
        bool ret = (DateTime.Now - lastExecuted) > reoccurrence;
        if (ret && willBeExecuted)
            lastExecuted = DateTime.Now;
        return ret;
    }

    public enum Task
    {
        UpdatePublications,
        UpdateChapters,
        DownloadNewChapters
    }
}