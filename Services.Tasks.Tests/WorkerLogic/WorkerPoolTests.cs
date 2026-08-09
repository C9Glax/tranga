using Common.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services.Tasks.Database;
using Services.Tasks.Tests.Helpers;
using Services.Tasks.TaskTypes;
using Services.Tasks.WorkerLogic;

namespace Services.Tasks.Tests.WorkerLogic;

public class WorkerPoolTests : TrangaTest
{
    private static readonly TimeSpan FastScaleCheckInterval = TimeSpan.FromMilliseconds(20);
    private static readonly TimeSpan ShortIdleRetirementTimeout = TimeSpan.FromMilliseconds(30);

    [Fact]
    public async Task ScaleUp_SpawnsWorkersUpToMax_WhenBacklogAndNoIdleWorkers()
    {
        IServiceProvider services = BuildServices();
        TaskQueue queue = new();
        for (int i = 0; i < 5; i++)
            await queue.AddTaskToQueue(new SlowTestTask(), ct);

        WorkerPool pool = new(queue, services, NoOpLoggerFactory.Instance, NoOpLogger<WorkerPool>.Instance,
            scaleCheckInterval: FastScaleCheckInterval, idleRetirementTimeout: ShortIdleRetirementTimeout,
            workersMin: 1, workersMax: 3);

        await pool.StartAsync(ct);
        try
        {
            await AsyncAssert.WaitUntil(() => pool.WorkerCount == 3, TimeSpan.FromSeconds(5));
            Assert.Equal(3, pool.WorkerCount);
        }
        finally
        {
            await pool.StopAsync(ct);
        }
    }

    [Fact]
    public async Task ScaleUp_NeverExceedsMax_EvenWithLargeBacklog()
    {
        IServiceProvider services = BuildServices();
        TaskQueue queue = new();
        for (int i = 0; i < 20; i++)
            await queue.AddTaskToQueue(new SlowTestTask(), ct);

        WorkerPool pool = new(queue, services, NoOpLoggerFactory.Instance, NoOpLogger<WorkerPool>.Instance,
            scaleCheckInterval: FastScaleCheckInterval, idleRetirementTimeout: ShortIdleRetirementTimeout,
            workersMin: 1, workersMax: 2);

        await pool.StartAsync(ct);
        try
        {
            await AsyncAssert.WaitUntil(() => pool.WorkerCount == 2, TimeSpan.FromSeconds(5));
            // Give it a few more scale-check ticks - it must settle at, not exceed, Max.
            await Task.Delay(FastScaleCheckInterval * 5, ct);
            Assert.Equal(2, pool.WorkerCount);
        }
        finally
        {
            await pool.StopAsync(ct);
        }
    }

    [Fact]
    public async Task ScaleDown_RetiresIdleWorkersAfterTimeout_ButNeverBelowMin()
    {
        IServiceProvider services = BuildServices();
        TaskQueue queue = new();
        for (int i = 0; i < 3; i++)
            await queue.AddTaskToQueue(new FastTestTask(), ct);

        WorkerPool pool = new(queue, services, NoOpLoggerFactory.Instance, NoOpLogger<WorkerPool>.Instance,
            scaleCheckInterval: FastScaleCheckInterval, idleRetirementTimeout: ShortIdleRetirementTimeout,
            workersMin: 1, workersMax: 3);

        await pool.StartAsync(ct);
        try
        {
            // Wait for the (fast, self-completing) backlog to be drained and idle workers to retire back down.
            await AsyncAssert.WaitUntil(() => pool.WorkerCount == 1, TimeSpan.FromSeconds(5));
            Assert.Equal(1, pool.WorkerCount);

            // Never below Min even after further idle time.
            await Task.Delay(ShortIdleRetirementTimeout * 5, ct);
            Assert.Equal(1, pool.WorkerCount);
        }
        finally
        {
            await pool.StopAsync(ct);
        }
    }

    [Fact]
    public async Task WipesWorkerTable_OnStartAndStop()
    {
        IServiceProvider services = BuildServices();
        using (IServiceScope seedScope = services.CreateScope())
        {
            TasksContext seedCtx = seedScope.ServiceProvider.GetRequiredService<TasksContext>();
            await seedCtx.Workers.AddAsync(new DbWorker(), ct);
            await seedCtx.SaveChangesAsync(ct);
        }

        TaskQueue queue = new();
        WorkerPool pool = new(queue, services, NoOpLoggerFactory.Instance, NoOpLogger<WorkerPool>.Instance,
            scaleCheckInterval: FastScaleCheckInterval, idleRetirementTimeout: ShortIdleRetirementTimeout,
            workersMin: 1, workersMax: 1);

        await pool.StartAsync(ct);
        // BackgroundService.StartAsync only guarantees ExecuteAsync has been scheduled, not that it has actually
        // begun running - stopping immediately with no yield in between can (rarely, under thread-pool pressure)
        // have StopAsync return before ExecuteAsync ever ran, leaving the seed row un-wiped. Real hosts never call
        // StopAsync this fast after StartAsync, so wait for the pool to actually come up first, like the other tests.
        await AsyncAssert.WaitUntil(() => pool.WorkerCount == 1, TimeSpan.FromSeconds(5));
        await pool.StopAsync(ct);

        using IServiceScope assertScope = services.CreateScope();
        TasksContext assertCtx = assertScope.ServiceProvider.GetRequiredService<TasksContext>();
        Assert.Empty(await assertCtx.Workers.ToListAsync(ct));
    }

    private static IServiceProvider BuildServices()
    {
        string dbPath = Path.Combine(Path.GetTempPath(), "TrangaTests", "Services.Tasks.Tests", $"{Guid.NewGuid():N}.db");
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        ServiceCollection services = new();
        services.AddDbContext<TasksContext>(options => options.UseSqlite($"Data Source={dbPath}"));
        ServiceProvider provider = services.BuildServiceProvider();

        using IServiceScope scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<TasksContext>().Database.EnsureCreated();

        return provider;
    }

    private sealed class SlowTestTask() : RunOnceTask(Guid.CreateVersion7())
    {
        protected override async Task RunAsync(IServiceScope scope, ILogger logger, CancellationToken stoppingToken) =>
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        protected override void RefreshScope(IServiceScope scope)
        {
        }
    }

    /// <summary>
    /// Takes just long enough (relative to <see cref="FastScaleCheckInterval"/>) that several can be in flight at
    /// once - long enough to provoke a scale-up, short enough that all workers go idle again well within the test's timeout.
    /// </summary>
    private sealed class FastTestTask() : RunOnceTask(Guid.CreateVersion7())
    {
        protected override async Task RunAsync(IServiceScope scope, ILogger logger, CancellationToken stoppingToken) =>
            await Task.Delay(TimeSpan.FromMilliseconds(50), stoppingToken);

        protected override void RefreshScope(IServiceScope scope)
        {
        }
    }
}
