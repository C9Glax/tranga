using Services.Tasks.WorkerLogic;

namespace Services.Tasks.Entities;

public sealed record Worker
{
    /// <summary>
    /// Id of Worker
    /// </summary>
    public required Guid WorkerId { get; init; }

    /// <summary>
    /// Current activity state of the Worker
    /// </summary>
    public required WorkerStatus Status { get; init; }

    /// <summary>
    /// Id of the Task currently being executed by this Worker, if any
    /// </summary>
    public Guid? CurrentTaskId { get; init; }

    /// <summary>
    /// TaskTypeId of the Task currently being executed by this Worker, if any
    /// </summary>
    public Guid? CurrentTaskTypeId { get; init; }

    /// <summary>
    /// When this Worker was started
    /// </summary>
    public required DateTimeOffset StartedAt { get; init; }

    /// <summary>
    /// Last time this Worker reported in
    /// </summary>
    public required DateTimeOffset LastHeartbeat { get; init; }
}
