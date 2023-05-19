using Newtonsoft.Json;

namespace Tranga;

/// <summary>
/// Stores information on Task
/// </summary>
public class TrangaTask
{
    // ReSharper disable once CommentTypo ...tell me why!
    // ReSharper disable once MemberCanBePrivate.Global I want it thaaat way
    public TimeSpan reoccurrence { get; }
    public DateTime lastExecuted { get; set; }
    public string connectorName { get; }
    public Task task { get; }
    public Publication? publication { get; }
    public string language { get; }
    [JsonIgnore]public bool isBeingExecuted { get; set; }

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

    /// <summary>
    /// 
    /// </summary>
    /// <returns>True if elapsed time since last execution is greater than set interval</returns>
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

    public override string ToString()
    {
        return $"{task}\t{lastExecuted}\t{reoccurrence}\t{(isBeingExecuted ? "running" : "waiting")}\t{connectorName}\t{publication?.sortName}";
    }
}