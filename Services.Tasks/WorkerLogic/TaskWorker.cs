using Services.Tasks.TaskTypes;
using Settings;

namespace Services.Tasks.WorkerLogic;

/// <summary>
/// A Worker that fetches work from <see cref="TaskQueue"/> and executes <see cref="TaskBase.ExecuteAsync"/>
/// </summary>
internal sealed class TaskWorker(TaskQueue queue, IServiceProvider serviceProvider, ILogger<TaskWorker> logger) : BackgroundService
{
    private Guid WorkerId = Guid.CreateVersion7();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("{WorkerId} running.", WorkerId);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (await queue.GetNextTask(stoppingToken) is { } workItem)
                {
                    logger.LogInformation("{workItem} running.", workItem);
                    await workItem.ExecuteAsync(serviceProvider.CreateScope(), logger, stoppingToken);
                }
                else Thread.Sleep(Constants.WorkerPickupWorkTimeout);
            }
            catch (OperationCanceledException)
            {
                // Prevent throwing if stoppingToken was signaled
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Worker {WorkerId} ran into an exception executing:", WorkerId);
            }
        }
    }
}