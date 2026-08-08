using Services.Tasks.Tests.Helpers;
using Services.Tasks.TaskTypes;

namespace Services.Tasks.Tests.TaskTypes;

public class RunOnceTaskTests
{
    [Fact]
    public async Task ExecuteAsync_SetsHasRun_OnSuccess()
    {
        TestRunOnceTask task = TestTask.Create<TestRunOnceTask>();

        await task.ExecuteAsync(new NoOpServiceScope(), NoOpLogger.Instance, CancellationToken.None);

        Assert.True(task.HasRun);
        Assert.Equal(TaskState.Completed, task.Status);
    }

    [Fact]
    public async Task ExecuteAsync_SetsHasRun_EvenWhenRunAsyncThrows()
    {
        ThrowingTestRunOnceTask task = new();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            task.ExecuteAsync(new NoOpServiceScope(), NoOpLogger.Instance, CancellationToken.None));

        Assert.True(task.HasRun);
        Assert.Equal(TaskState.Failed, task.Status);
    }
}
