namespace Services.Tasks.TaskTypes;

/// <summary>
/// A Periodic Task that runs after <see cref="Interval"/> has passed since <see cref="LastRun"/>
/// </summary>
/// <param name="taskTypeId"><inheritdoc cref="TaskBase" path="tagpath[@name='taskTypeId']"/></param>
internal abstract class PeriodicTask(Guid taskTypeId) : TaskBase(TaskType.PeriodicTask, taskTypeId)
{
    internal abstract TimeSpan Interval { get; init; }
    
    internal DateTimeOffset LastRun { get; set; }
    
    internal override async Task ExecuteAsync(IServiceScope scope, ILogger logger, CancellationToken stoppingToken)
    {
        logger.LogDebug("Task running.");
        LastRun = DateTimeOffset.UtcNow;
        await base.ExecuteAsync(scope, logger, stoppingToken);
        logger.LogDebug("Task finished.");
    }
}