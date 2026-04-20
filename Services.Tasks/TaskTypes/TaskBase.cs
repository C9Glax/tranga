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

    internal virtual Task ExecuteAsync(IServiceScope scope, ILogger logger, CancellationToken stoppingToken)
    {
        RefreshScope(scope);
        return RunAsync(scope, logger, stoppingToken);
    }

    private protected abstract Task RunAsync(IServiceScope scope, ILogger logger, CancellationToken stoppingToken);

    /// <summary>
    /// Get all required Services from the scope.<br />
    /// RefreshScope is called automatically when the Task starts
    /// </summary>
    private protected abstract void RefreshScope(IServiceScope scope);

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