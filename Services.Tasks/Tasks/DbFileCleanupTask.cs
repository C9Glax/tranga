using Services.Tasks.Database;

namespace Services.Tasks.Tasks;

internal class DbFileCleanupTask(Context ctx, Guid taskId) : PeriodicTask(ctx, taskId, Guid.Parse("ded1e7d1-ec8e-4795-910a-80bdd0d797d5"))
{
    protected override Task RunAsync(CancellationToken stoppingToken)
    {
        throw new NotImplementedException();
    }
}