using Services.Tasks.WorkerLogic;

namespace Services.Tasks.TaskTypes;

internal abstract class RunOnceTask(Guid taskTypeId) : TaskBase(TaskType.RunOnceTask, taskTypeId)
{
    internal bool HasRun { get; set; }
    
    internal override async Task ExecuteAsync(IServiceScope scope, CancellationToken stoppingToken)
    {
        await base.ExecuteAsync(scope, stoppingToken);
        TasksCollection.RunOnceTasks.TryRemove(TaskId, out _);
    }
}