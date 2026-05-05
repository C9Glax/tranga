namespace Services.Tasks.TaskTypes;

/// <summary>
/// A Task that runs only once
/// </summary>
/// <param name="taskTypeId"><inheritdoc cref="TaskBase" path="tagpath[@name='taskTypeId']"/></param>
internal abstract class RunOnceTask(Guid taskTypeId) : TaskBase(TaskType.RunOnceTask, taskTypeId)
{
    internal override async Task ExecuteAsync(IServiceScope scope, ILogger logger, CancellationToken stoppingToken)
    {
        await base.ExecuteAsync(scope, logger, stoppingToken);
        logger.LogDebug("Task finished.");
    }
}