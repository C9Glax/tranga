using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Services.Tasks.Tests.Helpers;
using Services.Tasks.TaskTypes;

namespace Services.Tasks.Tests.TaskTypes;

public class PeriodicTaskTests
{
    [Fact]
    public async Task ExecuteAsync_SetsStatusCompleted_OnSuccess()
    {
        TestPeriodicTask task = TestTask.Create<TestPeriodicTask>();

        await task.ExecuteAsync(new NoOpServiceScope(), NoOpLogger.Instance, CancellationToken.None);

        Assert.Equal(TaskState.Completed, task.Status);
    }

    [Fact]
    public async Task ExecuteAsync_StillLogsFinished_WhenRunAsyncThrows()
    {
        ThrowingTestPeriodicTask task = new();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            task.ExecuteAsync(new NoOpServiceScope(), NoOpLogger.Instance, CancellationToken.None));

        Assert.Equal(TaskState.Failed, task.Status);
    }

    private sealed class ThrowingTestPeriodicTask() : PeriodicTask(Guid.CreateVersion7())
    {
        internal override TimeSpan Interval { get; init; } = TimeSpan.FromMinutes(1);

        protected override Task RunAsync(IServiceScope scope, ILogger logger, CancellationToken stoppingToken) =>
            throw new InvalidOperationException("boom");

        protected override void RefreshScope(IServiceScope scope)
        {
        }
    }
}
