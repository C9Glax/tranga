using Newtonsoft.Json;

namespace Tranga;

/// <summary>
/// Stores information on Task
/// </summary>
public class TrangaTask
{
    // ReSharper disable once CommentTypo ...Tell me why!
    // ReSharper disable once MemberCanBePrivate.Global I want it thaaat way
    public TimeSpan reoccurrence { get; }
    public DateTime lastExecuted { get; set; }
    public string? connectorName { get; }
    public Task task { get; }
    public Publication? publication { get; }
    public string language { get; }
    [JsonIgnore]public bool isBeingExecuted { get; set; }

    public TrangaTask(Task task, string? connectorName, Publication? publication, TimeSpan reoccurrence, string language = "")
    {
        if(task != Task.UpdateKomgaLibrary && connectorName is null)
            throw new ArgumentException($"connectorName can not be null for task {task}");
        
        if (publication is null && task != Task.UpdatePublications && task != Task.UpdateKomgaLibrary)
            throw new ArgumentException($"Publication can not be null for task {task}");
        
        this.publication = publication;
        this.reoccurrence = reoccurrence;
        this.lastExecuted = DateTime.Now.Subtract(reoccurrence);
        this.connectorName = connectorName;
        this.task = task;
        this.language = language;
    }

    /// <returns>True if elapsed time since last execution is greater than set interval</returns>
    public bool ShouldExecute()
    {
        return DateTime.Now.Subtract(this.lastExecuted) > reoccurrence;
    }

    public enum Task
    {
        UpdatePublications,
        UpdateChapters,
        DownloadNewChapters,
        UpdateKomgaLibrary
    }

    public override string ToString()
    {
        return $"{task}\t{lastExecuted}\t{reoccurrence}\t{(isBeingExecuted ? "running" : "waiting")}\t{connectorName}\t{publication?.sortName}";
    }
}