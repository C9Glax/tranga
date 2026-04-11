using Microsoft.EntityFrameworkCore;
using Services.Tasks.Database;

namespace Services.Tasks.TaskTypes;

internal abstract class PeriodicTask(Guid taskId, Guid taskTypeId) : TaskBase(TaskType.PeriodicTask, taskTypeId)
{
    internal abstract TimeSpan Interval { get; init; }
    
    internal sealed override async Task ExecuteAsync(IServiceScope scope, CancellationToken stoppingToken)
    {
        RefreshContext(scope);
        await RunAsync(stoppingToken);
        await Context!.Tasks.FilterTasks<DbPeriodicTask>(taskId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.LastRun, DateTimeOffset.UtcNow), stoppingToken);
    }

    private protected abstract Task RunAsync(CancellationToken stoppingToken);
}