using Microsoft.EntityFrameworkCore;
using Services.Tasks.Database;

namespace Services.Tasks;

internal abstract class PeriodicTask(Context context, Guid taskId, Guid taskTypeId) : TaskBase(TaskType.PeriodicTask, taskTypeId)
{
    protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await context.Tasks.FilterTasks<DbPeriodicTask>(taskId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.LastRun, DateTimeOffset.UtcNow), stoppingToken);
        await RunAsync(stoppingToken);
    }

    protected abstract Task RunAsync(CancellationToken stoppingToken);
}