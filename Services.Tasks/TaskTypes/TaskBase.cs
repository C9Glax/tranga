namespace Services.Tasks.TaskTypes;

internal abstract class TaskBase(TaskType t, Guid taskTypeId)
{
    public Guid TaskId { get; init; } = Guid.CreateVersion7();
    
    public int Priority { get; set; } = 0;
    
    public Guid TaskTypeId { get; init; } = taskTypeId;

    internal readonly TaskType TaskType = t;

    internal virtual Task ExecuteAsync(IServiceScope scope, CancellationToken stoppingToken)
    {
        RefreshScope(scope);
        return RunAsync(scope, stoppingToken);
    }

    private protected abstract Task RunAsync(IServiceScope scope, CancellationToken stoppingToken);

    /// <summary>
    /// Get all required Services from the scope.<br />
    /// RefreshScope is called automatically when the Task starts
    /// </summary>
    private protected abstract void RefreshScope(IServiceScope scope);

    public override string ToString() => $"{base.ToString()} - Priority {Priority} - TaskType {TaskTypeId}";
}

public enum TaskType : byte
{
    PeriodicTask = 0,
    RunOnceTask = 1
}