using Microsoft.EntityFrameworkCore;
using Services.Tasks.Database;

namespace Services.Tasks;

internal abstract class RunOnceTask(Context context, Guid taskId, Guid taskTypeId): TaskBase(TaskType.RunOnceTask, taskTypeId)
{
    protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunAsync(stoppingToken);
        await context.Tasks.FilterTasks<DbRunOnceTask>(taskId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.HasRun, true), stoppingToken);
    }

    protected abstract Task RunAsync(CancellationToken stoppingToken);
}