using Services.Tasks.WorkerLogic;

namespace Services.Tasks.TaskTypes;

/// <summary>
/// A Task that runs only once
/// </summary>
/// <param name="taskTypeId"><inheritdoc cref="TaskBase" path="tagpath[@name='taskTypeId']"/></param>
internal abstract class RunOnceTask(Guid taskTypeId) : TaskBase(TaskType.RunOnceTask, taskTypeId)
{
    internal bool HasRun { get; set; }
    
    internal override async Task ExecuteAsync(IServiceScope scope, ILogger logger, CancellationToken stoppingToken)
    {
        logger.LogDebug("Task running.");
        await base.ExecuteAsync(scope, logger, stoppingToken);
        logger.LogTrace("Removing Task...");
        TasksCollection.RunOnceTasks.TryRemove(TaskId, out _);
        logger.LogDebug("Task finished.");
    }
}