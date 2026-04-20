using Services.Tasks.TaskTypes;
using Settings;

namespace Services.Tasks.WorkerLogic;

/// <summary>
/// Adds <see cref="PeriodicTask"/> to <see cref="TaskQueue"/> once the <see cref="PeriodicTask"/> is scheduled to run next.
/// </summary>
internal sealed class PeriodicTaskScheduler(TaskQueue taskQueue, ILogger<PeriodicTaskScheduler> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PeriodicTaskScheduler running.");
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogTrace("Getting due tasks...");
            List<TaskBase> dueTasks = TasksCollection.PeriodicTasks
                .Where(t => t.LastRun + t.Interval < DateTimeOffset.UtcNow)
                .Concat<TaskBase>(TasksCollection.RunOnceTasks.Values)
                .ToList();

            foreach (TaskBase task in dueTasks)
            {
                if(taskQueue.ContainsTask(task.TaskId))
                    continue;
                await taskQueue.AddTaskToQueue(task, stoppingToken);
                logger.LogInformation("Added Task {task} to queue.", task);
            }
            
            Thread.Sleep(Constants.SchedulerCreateWorkTimeout);
        }
    }
}