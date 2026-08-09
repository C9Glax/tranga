using Common.Settings;
using Services.Tasks.TaskTypes;

namespace Services.Tasks.WorkerLogic;

/// <summary>
/// A Worker that fetches work from <see cref="TaskQueue"/> and executes <see cref="TaskBase.ExecuteAsync"/>.
/// Owned and run by <see cref="WorkerPool"/>, which dynamically starts/stops instances of this class rather
/// than registering a fixed number of them as hosted services.
/// </summary>
internal sealed class TaskWorker(Guid workerId, TaskQueue queue, IServiceProvider serviceProvider, WorkerRuntimeState state, ILogger logger)
{
    internal Guid WorkerId { get; } = workerId;

    /// <summary>
    /// Set by <see cref="WorkerPool"/> to ask this worker to stop after its current (or next) idle check.
    /// Only ever observed between tasks - a Task that has already been dequeued always runs to completion,
    /// so retirement can never abort in-flight work.
    /// </summary>
    internal volatile bool RetireRequested;

    internal async Task RunAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("{WorkerId} running.", WorkerId);
        while (!stoppingToken.IsCancellationRequested && !RetireRequested)
        {
            try
            {
                if (await queue.GetNextTask(stoppingToken) is { } workItem)
                {
                    state.OnTaskPickedUp(workItem);
                    logger.LogInformation("{workItem} running.", workItem);
                    ILogger taskLogger = new TaskCapturingLogger(logger, workItem);
                    await workItem.ExecuteAsync(serviceProvider.CreateScope(), taskLogger, stoppingToken);
                    state.OnTaskFinished();
                }
                else
                {
                    await Task.Delay(Constants.WorkerPickupWorkTimeout, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Prevent throwing if stoppingToken was signaled
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Worker {WorkerId} ran into an exception executing:", WorkerId);
                state.OnTaskFinished();
            }
        }
        logger.LogInformation("{WorkerId} retiring.", WorkerId);
        state.OnRetired();
    }
}
