using Services.Tasks.WorkerLogic;

namespace Services.Tasks.Database;

/// <summary>
/// Postgres-persisted snapshot of a currently-running <see cref="TaskWorker"/>, refreshed by
/// <see cref="WorkerPool"/> for observability. The table is wiped on process startup and shutdown - this project
/// runs a single <c>Services.Tasks</c> instance, so any row still present at startup is unconditionally stale.
/// </summary>
public sealed record DbWorker
{
    public Guid WorkerId { get; init; } = Guid.CreateVersion7();

    public WorkerStatus Status { get; init; } = WorkerStatus.Idle;

    public Guid? CurrentTaskId { get; init; }

    public Guid? CurrentTaskTypeId { get; init; }

    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset LastHeartbeat { get; init; } = DateTimeOffset.UtcNow;
}
