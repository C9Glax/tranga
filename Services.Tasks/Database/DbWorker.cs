using Services.Tasks.WorkerLogic;

namespace Services.Tasks.Database;

/// <summary>
/// Postgres-persisted snapshot of a currently-running <see cref="TaskWorker"/>, refreshed by
/// <see cref="WorkerPool"/> for observability. The table is wiped on process startup and shutdown - this project
/// runs a single <c>Services.Tasks</c> instance, so any row still present at startup is unconditionally stale.
/// </summary>
public sealed record DbWorker
{
    /// <summary>Id of the worker this row is a snapshot of.</summary>
    public Guid WorkerId { get; init; } = Guid.CreateVersion7();

    /// <summary>Activity state of the worker at the time this row was last refreshed.</summary>
    public WorkerStatus Status { get; init; } = WorkerStatus.Idle;

    /// <summary>Id of the Task currently being executed by this worker, if any.</summary>
    public Guid? CurrentTaskId { get; init; }

    /// <summary>TaskTypeId of the Task currently being executed by this worker, if any.</summary>
    public Guid? CurrentTaskTypeId { get; init; }

    /// <summary>When this worker was started.</summary>
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Last time this row was refreshed by <see cref="WorkerPool"/>.</summary>
    public DateTimeOffset LastHeartbeat { get; init; } = DateTimeOffset.UtcNow;
}
