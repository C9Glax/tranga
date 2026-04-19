using Services.Tasks.TaskTypes;
using Settings;

namespace Services.Tasks.WorkerLogic;

internal sealed class PeriodicTaskScheduler(TaskQueue taskQueue, ILogger<TaskWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PeriodicTaskScheduler running.");
        while (!stoppingToken.IsCancellationRequested)
        {
            List<PeriodicTask> dueTasks = TasksCollection.PeriodicTasks.Where(t => t.LastRun + t.Interval < DateTimeOffset.UtcNow).ToList();

            foreach (PeriodicTask dbPeriodicTask in dueTasks)
            {
                await taskQueue.AddTaskToQueue(dbPeriodicTask, stoppingToken);
                logger.LogInformation("Added Task {TaskId} to queue.", dbPeriodicTask.TaskId);
            }
            
            Thread.Sleep(Constants.SchedulerCreateWorkTimeout);
        }
    }
}