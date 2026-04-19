namespace Services.Tasks.TaskTypes;

internal abstract class PeriodicTask(Guid taskTypeId) : TaskBase(TaskType.PeriodicTask, taskTypeId)
{
    internal abstract TimeSpan Interval { get; init; }
    
    internal DateTimeOffset LastRun { get; set; }
    
    internal override async Task ExecuteAsync(IServiceScope scope, CancellationToken stoppingToken)
    {
        LastRun = DateTimeOffset.UtcNow;
        await base.ExecuteAsync(scope, stoppingToken);
    }
}