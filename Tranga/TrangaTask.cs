namespace Tranga;

public struct TrangaTask
{
    public TimeSpan reoccurrence { get; }
    public DateTime lastExecuted { get; set; }
    public string connectorName { get; }
    public Task task { get; }
    public Publication? publication { get; }
    public string language { get; }

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