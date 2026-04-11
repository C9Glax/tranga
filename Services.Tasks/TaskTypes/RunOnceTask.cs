using Microsoft.EntityFrameworkCore;
using Services.Tasks.Database;

namespace Services.Tasks.TaskTypes;

internal abstract class RunOnceTask(Guid taskId, Guid taskTypeId): TaskBase(TaskType.RunOnceTask, taskTypeId)
{
    internal sealed override async Task ExecuteAsync(IServiceScope scope, CancellationToken stoppingToken)
    {
        RefreshContext(scope);
        await RunAsync(stoppingToken);
        await Context!.Tasks.FilterTasks<DbRunOnceTask>(taskId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.HasRun, true), stoppingToken);
    }

    private protected abstract Task RunAsync(CancellationToken stoppingToken);
}