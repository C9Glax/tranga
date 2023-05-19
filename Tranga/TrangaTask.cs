namespace Tranga;

public class TrangaTask
{
    public TimeSpan reoccurrence { get; }
    public DateTime lastExecuted { get; set; }
    public string connectorName { get; }
    public Task task { get; }
    public Publication? publication { get; }
    public string language { get; }

    public TrangaTask(string connectorName, Task task, Publication? publication, TimeSpan reoccurrence, string language = "")
    {
        if (task != Task.UpdatePublications && publication is null)
            throw new ArgumentException($"Publication has to be not null for task {task}");
        this.publication = publication;
        this.reoccurrence = reoccurrence;
        this.lastExecuted = DateTime.Now.Subtract(reoccurrence);
        this.connectorName = connectorName;
        this.task = task;
        this.language = language;
    }

    public bool ShouldExecute()
    {
        return DateTime.Now.Subtract(this.lastExecuted) > reoccurrence;
    }

    public enum Task
    {
        UpdatePublications,
        UpdateChapters,
        DownloadNewChapters
    }
}