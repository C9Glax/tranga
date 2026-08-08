namespace Services.Tasks.WorkerLogic;

/// <summary>
/// Best-effort, unsynchronized snapshot of a <see cref="TaskWorker"/>'s current activity. Written only by the
/// worker's own run loop, read only by <see cref="WorkerPool"/> for scaling decisions and Postgres heartbeats -
/// same unsynchronized-field looseness the rest of this project already uses for <c>TaskBase.LastRun</c>/<c>HasRun</c>.
/// </summary>
internal sealed class WorkerRuntimeState(Guid workerId)
{
    internal Guid WorkerId { get; } = workerId;

    internal DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;

    internal WorkerStatus Status { get; private set; } = WorkerStatus.Idle;

    internal Guid? CurrentTaskId { get; private set; }

    internal Guid? CurrentTaskTypeId { get; private set; }

    /// <summary>
    /// When the worker last became idle, or when it was created if it has never run a Task. Null while busy.
    /// </summary>
    internal DateTimeOffset? IdleSince { get; private set; } = DateTimeOffset.UtcNow;

    internal void OnTaskPickedUp(TaskTypes.TaskBase task)
    {
        Status = WorkerStatus.Busy;
        CurrentTaskId = task.TaskId;
        CurrentTaskTypeId = task.TaskTypeId;
        IdleSince = null;
    }

    internal void OnTaskFinished()
    {
        Status = WorkerStatus.Idle;
        CurrentTaskId = null;
        CurrentTaskTypeId = null;
        IdleSince = DateTimeOffset.UtcNow;
    }

    internal void OnRetired() => Status = WorkerStatus.Retiring;
}

/// <summary>
/// Activity state of a <see cref="TaskWorker"/>, mirrored into Postgres via <see cref="Database.DbWorker"/> for observability.
/// </summary>
public enum WorkerStatus
{
    Idle,
    Busy,
    Retiring
}
