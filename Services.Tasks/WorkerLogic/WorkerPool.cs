using Common.Settings;
using Microsoft.EntityFrameworkCore;
using Services.Tasks.Database;

namespace Services.Tasks.WorkerLogic;

/// <summary>
/// Owns a dynamically-sized pool of <see cref="TaskWorker"/>s, scaling between <see cref="EnvVars.WorkersMin"/>
/// and <see cref="EnvVars.WorkersMax"/> based on <see cref="TaskQueue.ReadyCount"/>, and mirrors pool membership
/// into Postgres (<see cref="DbWorker"/>) for observability. Replaces the old fixed-count
/// <c>AddHostedService&lt;TaskWorker&gt;()</c> registration.
/// </summary>
internal sealed class WorkerPool(
    TaskQueue queue,
    IServiceProvider serviceProvider,
    ILoggerFactory loggerFactory,
    ILogger<WorkerPool> logger,
    TimeSpan? scaleCheckInterval = null,
    TimeSpan? idleRetirementTimeout = null,
    int? workersMin = null,
    int? workersMax = null) : BackgroundService
{
    // Overridable only for tests, so scale-up/down can be exercised without waiting on the real (multi-second/minute)
    // timings or being at the mercy of the test process's actual EnvVars.WorkersMin/Max (CPU-count-dependent).
    private readonly TimeSpan _scaleCheckInterval = scaleCheckInterval ?? Constants.WorkerPoolScaleCheckInterval;
    private readonly TimeSpan _idleRetirementTimeout = idleRetirementTimeout ?? Constants.WorkerIdleRetirementTimeout;
    private readonly int _workersMin = workersMin ?? EnvVars.WorkersMin;
    private readonly int _workersMax = workersMax ?? EnvVars.WorkersMax;

    private readonly List<(TaskWorker Worker, WorkerRuntimeState State, Task RunLoop)> _workers = [];

    // Tracked separately so a graceful shutdown still awaits workers that were already retiring when it began.
    private readonly List<Task> _retiredLoops = [];

    internal int WorkerCount => _workers.Count;

    internal int IdleWorkerCount => _workers.Count(w => w.State.Status == WorkerStatus.Idle);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("WorkerPool starting with min={Min} max={Max}.", _workersMin, _workersMax);
        await WipeWorkerTableAsync(CancellationToken.None);

        for (int i = 0; i < _workersMin; i++)
            SpawnWorker(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            ScaleUpIfNeeded(stoppingToken);
            ScaleDownIdleWorkers();
            await PersistHeartbeatsAsync(CancellationToken.None);

            try
            {
                await Task.Delay(_scaleCheckInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Host is stopping
            }
        }

        await Task.WhenAll(_workers.Select(w => w.RunLoop).Concat(_retiredLoops));

        try
        {
            await WipeWorkerTableAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to clear Workers table on shutdown.");
        }
    }

    private void SpawnWorker(CancellationToken stoppingToken)
    {
        Guid id = Guid.CreateVersion7();
        WorkerRuntimeState state = new(id);
        TaskWorker worker = new(id, queue, serviceProvider, state, loggerFactory.CreateLogger<TaskWorker>());
        Task runLoop = worker.RunAsync(stoppingToken);
        _workers.Add((worker, state, runLoop));
        logger.LogDebug("Spawned worker {WorkerId}. Pool size is now {Count}.", id, _workers.Count);
    }

    private void ScaleUpIfNeeded(CancellationToken stoppingToken)
    {
        bool anyIdle = _workers.Any(w => w.State.Status == WorkerStatus.Idle);
        // Only scale up when no idle worker exists to pick up the backlog - TaskQueue.GetNextTask is poll-based,
        // so ReadyCount can be transiently >0 even with idle capacity available, and using it alone would thrash.
        if (!anyIdle && queue.ReadyCount > 0 && _workers.Count < _workersMax)
            SpawnWorker(stoppingToken);
    }

    private void ScaleDownIdleWorkers()
    {
        List<(TaskWorker Worker, WorkerRuntimeState State, Task RunLoop)> idleTooLong = _workers
            .Where(w => w.State.IdleSince is { } since && DateTimeOffset.UtcNow - since > _idleRetirementTimeout)
            .ToList();

        foreach (var w in idleTooLong)
        {
            if (_workers.Count <= _workersMin)
                break;

            w.Worker.RetireRequested = true;
            _workers.Remove(w);
            _retiredLoops.Add(w.RunLoop);
            logger.LogDebug("Retiring idle worker {WorkerId}. Pool size is now {Count}.", w.Worker.WorkerId, _workers.Count);
        }
    }

    private async Task WipeWorkerTableAsync(CancellationToken ct)
    {
        using IServiceScope scope = serviceProvider.CreateScope();
        TasksContext ctx = scope.ServiceProvider.GetRequiredService<TasksContext>();
        await ctx.Workers.ExecuteDeleteAsync(ct);
    }

    /// <summary>
    /// Overwrites the Workers table with the current in-memory pool snapshot. Simple delete-then-reinsert rather
    /// than incremental upserting - correct and simple given the pool is always small (bounded by
    /// <see cref="EnvVars.WorkersMax"/>) and single-instance, so there is no concurrent writer to race with.
    /// </summary>
    private async Task PersistHeartbeatsAsync(CancellationToken ct)
    {
        try
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            TasksContext ctx = scope.ServiceProvider.GetRequiredService<TasksContext>();

            await ctx.Workers.ExecuteDeleteAsync(ct);

            DbWorker[] rows = _workers.Select(w => new DbWorker
            {
                WorkerId = w.State.WorkerId,
                Status = w.State.Status,
                CurrentTaskId = w.State.CurrentTaskId,
                CurrentTaskTypeId = w.State.CurrentTaskTypeId,
                StartedAt = w.State.StartedAt,
                LastHeartbeat = DateTimeOffset.UtcNow
            }).ToArray();

            await ctx.Workers.AddRangeAsync(rows, ct);
            await ctx.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to persist worker heartbeats.");
        }
    }
}
