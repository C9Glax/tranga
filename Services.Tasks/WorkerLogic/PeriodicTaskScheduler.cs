using Microsoft.EntityFrameworkCore;
using Services.Tasks.Database;
using Services.Tasks.TaskTypes;

namespace Services.Tasks.WorkerLogic;

internal sealed class PeriodicTaskScheduler(TasksContext tasksContext, TaskQueue taskQueue, ILogger<TaskWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PeriodicTaskScheduler running.");
        while (!stoppingToken.IsCancellationRequested)
        {
            tasksContext.ChangeTracker.Clear();
            tasksContext.Tasks.Local.Clear();
            List<DbPeriodicTask> dbPeriodicTasks = await tasksContext.Tasks.FilterTasks<DbPeriodicTask>().ToListAsync(stoppingToken);
            List<DbPeriodicTask> dueTasks = dbPeriodicTasks.Where(t => t.LastRun + t.GetTaskTimeout() < DateTimeOffset.UtcNow).ToList();
            foreach (DbPeriodicTask dbPeriodicTask in dueTasks)
            {
                await tasksContext.Tasks.FilterTasks<DbPeriodicTask>(dbPeriodicTask.TaskId)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.LastRun, DateTimeOffset.UtcNow), stoppingToken);
                TaskBase task = dbPeriodicTask.CreateTaskFromDbTask();
                await taskQueue.AddTaskToQueue(task, stoppingToken);
                logger.LogInformation("Added Task {TaskId} to queue.", dbPeriodicTask.TaskId);
            }
        }
    }
}