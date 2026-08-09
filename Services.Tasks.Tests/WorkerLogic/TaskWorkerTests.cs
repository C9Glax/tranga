using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services.Tasks.Tests.Helpers;
using Services.Tasks.TaskTypes;
using Services.Tasks.WorkerLogic;

namespace Services.Tasks.Tests.WorkerLogic;

public class TaskWorkerTests
{
    [Fact]
    public async Task RunAsync_PicksUpQueuedTask_AndUpdatesRuntimeState()
    {
        TaskQueue queue = new();
        TestRunOnceTask task = TestTask.Create<TestRunOnceTask>();
        await queue.AddTaskToQueue(task, CancellationToken.None);

        IServiceProvider services = new ServiceCollection().BuildServiceProvider();
        WorkerRuntimeState state = new(Guid.CreateVersion7());
        TaskWorker worker = new(Guid.CreateVersion7(), queue, services, state, AlwaysEnabledNoOpLogger.Instance);

        using CancellationTokenSource cts = new();
        Task runLoop = worker.RunAsync(cts.Token);

        await AsyncAssert.WaitUntil(() => task.Status == TaskState.Completed, TimeSpan.FromSeconds(5));

        Assert.Equal(TaskState.Completed, task.Status);
        Assert.Equal(WorkerStatus.Idle, state.Status);
        Assert.NotEmpty(task.LogEntries);

        cts.Cancel();
        await runLoop;
    }

    [Fact]
    public async Task RunAsync_RetireRequested_DoesNotAbortInFlightTask()
    {
        TaskQueue queue = new();
        SlowTestTask task = new();
        await queue.AddTaskToQueue(task, CancellationToken.None);

        IServiceProvider services = new ServiceCollection().BuildServiceProvider();
        WorkerRuntimeState state = new(Guid.CreateVersion7());
        TaskWorker worker = new(Guid.CreateVersion7(), queue, services, state, NoOpLogger.Instance);

        Task runLoop = worker.RunAsync(CancellationToken.None);

        await AsyncAssert.WaitUntil(() => task.Status == TaskState.Running, TimeSpan.FromSeconds(5));

        // Ask the worker to retire while it is still executing the Task - retirement must not abort it.
        worker.RetireRequested = true;

        await AsyncAssert.WaitUntil(() => task.Status == TaskState.Completed, TimeSpan.FromSeconds(5));
        await runLoop;

        Assert.Equal(TaskState.Completed, task.Status);
        Assert.Equal(WorkerStatus.Retiring, state.Status);
    }

    private sealed class SlowTestTask() : RunOnceTask(Guid.CreateVersion7())
    {
        protected override async Task RunAsync(IServiceScope scope, ILogger logger, CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(300), stoppingToken);
        }

        protected override void RefreshScope(IServiceScope scope)
        {
        }
    }
}
