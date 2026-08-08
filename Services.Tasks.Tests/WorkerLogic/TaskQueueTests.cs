using Services.Tasks.TaskTypes;
using Services.Tasks.Tests.Helpers;
using Services.Tasks.WorkerLogic;

namespace Services.Tasks.Tests.WorkerLogic;

public class TaskQueueTests
{
    [Fact]
    public async Task Enqueue_AddsTaskToQueue()
    {
        TaskQueue queue = new();
        TestRunOnceTask task = TestTask.Create<TestRunOnceTask>(priority: 5);

        await queue.AddTaskToQueue(task, CancellationToken.None);

        Assert.True(queue.ContainsTask(task.TaskId));

        TaskBase? dequeued = await queue.GetNextTask(CancellationToken.None);

        Assert.Same(task, dequeued);
        Assert.False(queue.ContainsTask(task.TaskId));
    }

    [Fact]
    public async Task Enqueue_SetsTaskStatusToQueued()
    {
        TaskQueue queue = new();
        TestRunOnceTask task = TestTask.Create<TestRunOnceTask>(priority: 5);

        Assert.Equal(TaskState.Pending, task.Status);

        await queue.AddTaskToQueue(task, CancellationToken.None);

        Assert.Equal(TaskState.Queued, task.Status);
    }

    [Fact]
    public async Task ReadyCount_ReflectsQueueDepth()
    {
        TaskQueue queue = new();
        Assert.Equal(0, queue.ReadyCount);

        await queue.AddTaskToQueue(TestTask.Create<TestRunOnceTask>(priority: 1), CancellationToken.None);
        await queue.AddTaskToQueue(TestTask.Create<TestRunOnceTask>(priority: 2), CancellationToken.None);
        Assert.Equal(2, queue.ReadyCount);

        await queue.GetNextTask(CancellationToken.None);
        Assert.Equal(1, queue.ReadyCount);
    }

    [Fact]
    public async Task Dequeue_ReturnsHighestPriorityTaskFirst()
    {
        TaskQueue queue = new();
        TestRunOnceTask lowPriority = TestTask.Create<TestRunOnceTask>(priority: 1);
        TestRunOnceTask highPriority = TestTask.Create<TestRunOnceTask>(priority: 10);
        TestRunOnceTask samePriorityFirst = TestTask.Create<TestRunOnceTask>(priority: 5);
        TestRunOnceTask samePrioritySecond = TestTask.Create<TestRunOnceTask>(priority: 5);

        await queue.AddTaskToQueue(lowPriority, CancellationToken.None);
        await queue.AddTaskToQueue(highPriority, CancellationToken.None);
        await queue.AddTaskToQueue(samePriorityFirst, CancellationToken.None);
        await queue.AddTaskToQueue(samePrioritySecond, CancellationToken.None);

        TaskBase? first = await queue.GetNextTask(CancellationToken.None);
        TaskBase? second = await queue.GetNextTask(CancellationToken.None);
        TaskBase? third = await queue.GetNextTask(CancellationToken.None);
        TaskBase? fourth = await queue.GetNextTask(CancellationToken.None);
        TaskBase[] samePriorityTasks = [samePriorityFirst, samePrioritySecond];

        Assert.Same(lowPriority, first);
        Assert.Contains(second, samePriorityTasks);
        Assert.Contains(third, samePriorityTasks);
        Assert.NotSame(second, third);
        Assert.Same(highPriority, fourth);
    }

    [Fact]
    public async Task Dequeue_ReturnsNullWhenQueueIsEmpty()
    {
        TaskQueue queue = new();

        TaskBase? next = await queue.GetNextTask(CancellationToken.None);

        Assert.Null(next);
    }

    [Fact]
    public async Task Enqueue_HandlesDuplicateTasksAccordingToContract()
    {
        TaskQueue queue = new();
        TestRunOnceTask task = TestTask.Create<TestRunOnceTask>(priority: 3);

        await queue.AddTaskToQueue(task, CancellationToken.None);
        await queue.AddTaskToQueue(task, CancellationToken.None);

        TaskBase? next = await queue.GetNextTask(CancellationToken.None);
        TaskBase? after = await queue.GetNextTask(CancellationToken.None);

        Assert.Same(task, next);
        Assert.Null(after);
    }

    [Fact]
    public async Task Enqueue_IsSafeUnderConcurrentAccess()
    {
        // Arrange
        TaskQueue queue = new();
        var tasks = Enumerable.Range(0, 100)
            .Select(i => TestTask.Create<TestRunOnceTask>(priority: i % 10))
            .ToList();

        // Act
        var enqueueTasks = tasks.Select(task => queue.AddTaskToQueue(task, CancellationToken.None).AsTask()).ToList();
        await Task.WhenAll(enqueueTasks);

        // Assert - All tasks should be in the queue
        foreach (var task in tasks)
        {
            Assert.True(queue.ContainsTask(task.TaskId));
        }

        // Dequeue all tasks and verify count
        var dequeuedCount = 0;
        while (await queue.GetNextTask(CancellationToken.None) is not null)
        {
            dequeuedCount++;
        }
        Assert.Equal(tasks.Count, dequeuedCount);
    }
}
