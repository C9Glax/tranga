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
        IEnumerable<TaskBase> samePriorityTasks = [samePriorityFirst, samePrioritySecond];

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
}



