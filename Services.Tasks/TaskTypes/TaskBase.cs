using Services.Tasks.Database;

namespace Services.Tasks.TaskTypes;

internal abstract class TaskBase(TaskType t, Guid taskTypeId)
{
    public int Priority { get; set; } = 0;
    
    public Guid TaskTypeId { get; init; } = taskTypeId;

    internal readonly TaskType TaskType = t;
    
    protected Context? Context { get; private set; }

    internal abstract Task ExecuteAsync(IServiceScope scope, CancellationToken stoppingToken);

    /// <summary>
    /// Get all required Services from the scope.<br />
    /// RefreshScope is called called automatically when the Task starts
    /// </summary>
    private protected abstract void RefreshScope(IServiceScope scope);

    private protected void RefreshContext(IServiceScope scope)
    {
        Context = scope.ServiceProvider.GetRequiredService<Context>();
    }
}

public enum TaskType : byte
{
    PeriodicTask = 0,
    RunOnceTask = 1
}