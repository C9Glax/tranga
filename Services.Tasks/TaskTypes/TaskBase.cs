namespace Services.Tasks.TaskTypes;

/// <summary>
/// A Task
/// </summary>
/// <param name="t"><inheritdoc cref="Services.Tasks.TaskTypes.TaskType"/></param>
/// <param name="taskTypeId">A <b>unique</b> (across all <see cref="TaskBase"/>) that identifies what type of Task this is.</param>
internal abstract class TaskBase(TaskType t, Guid taskTypeId) : ITask
{
    public Guid TaskId { get; init; } = Guid.CreateVersion7();

    public int Priority { get; set; } = 0;

    public Guid TaskTypeId { get; init; } = taskTypeId;

    public TaskType TaskType { get; init; } = t;

    public DateTimeOffset? LastRun { get; set; } = null;

    public TaskState Status { get; set; } = TaskState.Pending;

    /// <summary>
    /// <see cref="TaskTypeId"/>s of Tasks that must reach a terminal <see cref="TaskState"/> (<see cref="TaskState.Completed"/>
    /// or <see cref="TaskState.Failed"/>) for the same correlation key (see <see cref="Services.Tasks.WorkerLogic.TaskDependencyResolver"/>) before
    /// this Task is allowed to run.
    /// </summary>
    internal virtual IReadOnlyCollection<Guid> DependsOnTaskTypeIds { get; } = [];

    internal virtual async Task ExecuteAsync(IServiceScope scope, ILogger logger, CancellationToken stoppingToken)
    {
        Status = TaskState.Running;
        try
        {
            LastRun = DateTimeOffset.UtcNow;
            RefreshScope(scope);
            logger.LogDebug("Task running.");

            // TODO Publish a "TaskRunningEvent"

            await RunAsync(scope, logger, stoppingToken);
            Status = TaskState.Completed;
        }
        catch
        {
            Status = TaskState.Failed;
            throw;
        }
    }

    protected abstract Task RunAsync(IServiceScope scope, ILogger logger, CancellationToken stoppingToken);

    /// <summary>
    /// Get all required Services from the scope.<br />
    /// RefreshScope is called automatically when the Task starts
    /// </summary>
    protected abstract void RefreshScope(IServiceScope scope);

    public override string ToString() => $"{base.ToString()} - {TaskType} {TaskTypeId} - Priority {Priority}";
}

/// <summary>
/// The type of the Task
/// </summary>
public enum TaskType : byte
{
    /// <summary>
    /// <inheritdoc cref="Services.Tasks.TaskTypes.PeriodicTask"/>
    /// </summary>
    PeriodicTask = 0,
    /// <summary>
    /// <inheritdoc cref="Services.Tasks.TaskTypes.RunOnceTask"/>
    /// </summary>
    RunOnceTask = 1
}

/// <summary>
/// The lifecycle state of a <see cref="TaskBase"/> instance.
/// </summary>
public enum TaskState : byte
{
    /// <summary>
    /// Not yet queued; either not due, or blocked on a dependency.
    /// </summary>
    Pending = 0,
    /// <summary>
    /// Due to run, but blocked on an unfinished dependency (see <see cref="TaskBase.DependsOnTaskTypeIds"/>).
    /// </summary>
    Blocked = 1,
    /// <summary>
    /// In the ready queue, waiting for a worker to pick it up.
    /// </summary>
    Queued = 2,
    /// <summary>
    /// Currently executing on a worker.
    /// </summary>
    Running = 3,
    /// <summary>
    /// Finished successfully.
    /// </summary>
    Completed = 4,
    /// <summary>
    /// Finished with an exception.
    /// </summary>
    Failed = 5
}