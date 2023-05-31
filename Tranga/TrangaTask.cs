using Newtonsoft.Json;

namespace Tranga;

/// <summary>
/// Stores information on Task
/// </summary>
public abstract class TrangaTask
{
    // ReSharper disable once CommentTypo ...Tell me why!
    // ReSharper disable once MemberCanBePrivate.Global I want it thaaat way
    public TimeSpan reoccurrence { get; }
    public DateTime lastExecuted { get; set; }
    public string? connectorName { get; }
    public Task task { get; }
    public Publication? publication { get; }
    public string? language { get; }
    [JsonIgnore]public ExecutionState state { get; set; }

    public enum ExecutionState
    {
        Waiting,
        Enqueued,
        Running
    };

    protected TrangaTask(Task task, string? connectorName, Publication? publication, TimeSpan reoccurrence, string? language = null)
    {
        this.publication = publication;
        this.reoccurrence = reoccurrence;
        this.lastExecuted = DateTime.Now.Subtract(reoccurrence);
        this.connectorName = connectorName;
        this.task = task;
        this.language = language;
    }

    /// <summary>
    /// Set state to running
    /// </summary>
    /// <param name="taskManager"></param>
    public abstract void Execute(TaskManager taskManager);

    /// <returns>True if elapsed time since last execution is greater than set interval</returns>
    public bool ShouldExecute()
    {
        return DateTime.Now.Subtract(this.lastExecuted) > reoccurrence && state is ExecutionState.Waiting;
    }

    public enum Task
    {
        DownloadNewChapters,
        UpdateKomgaLibrary
    }

    public override string ToString()
    {
        return $"{task}, {lastExecuted}, {reoccurrence}, {state} {(connectorName is not null ? $", {connectorName}" : "" )} {(publication is not null ? $", {publication?.sortName}": "")}";
    }
}