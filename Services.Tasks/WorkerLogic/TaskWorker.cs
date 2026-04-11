using Settings;

namespace Services.Tasks.WorkerLogic;

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
                if(await queue.GetNextTask(stoppingToken) is { } workItem)
                    await workItem.ExecuteAsync(serviceProvider.CreateScope(), stoppingToken);
                else Thread.Sleep(Constants.WorkerPickupWorkTimeout);
            }
            catch (OperationCanceledException)
            {
                // Prevent throwing if stoppingToken was signaled
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{WorkerId} ran into an exception:", WorkerId);
            }
        }
    }
}