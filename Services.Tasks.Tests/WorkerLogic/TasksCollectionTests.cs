using Services.Tasks.Tasks;
using Services.Tasks.TaskTypes;
using Services.Tasks.WorkerLogic;

namespace Services.Tasks.Tests.WorkerLogic;

public class TasksCollectionTests : IDisposable
{
    public void Dispose()
    {
        TasksCollection.PeriodicTasks.Clear();
        TasksCollection.RunOnceTasks.Clear();
    }

    [Fact]
    public void Add_RegistersTaskType()
    {
        TasksCollection.PeriodicTasks.Clear();
        TasksCollection.RunOnceTasks.Clear();

        PeriodicMangaChapterFetcherTask periodicTask = new();
        TasksCollection.PeriodicTasks.Add(periodicTask);

        Assert.Contains(periodicTask, TasksCollection.PeriodicTasks);
        Assert.Contains(periodicTask, TasksCollection.GetKnownTasks());
    }

    [Fact]
    public void Get_ReturnsRegisteredTaskByKey()
    {
        TasksCollection.PeriodicTasks.Clear();
        TasksCollection.RunOnceTasks.Clear();

        GetMangaChaptersTask runOnceTask = new(Guid.NewGuid());
        TasksCollection.RunOnceTasks.TryAdd(runOnceTask.TaskId, runOnceTask);

        TaskBase? task = TasksCollection.GetTask(runOnceTask.TaskId);

        Assert.Same(runOnceTask, task);
    }

    [Fact]
    public void Get_ReturnsNullForUnknownTask()
    {
        TasksCollection.PeriodicTasks.Clear();
        TasksCollection.RunOnceTasks.Clear();

        TaskBase? task = TasksCollection.GetTask(Guid.NewGuid());

        Assert.Null(task);
    }

    [Fact]
    public void Enumerate_ReturnsAllRegisteredTasks()
    {
        TasksCollection.PeriodicTasks.Clear();
        TasksCollection.RunOnceTasks.Clear();

        PeriodicMangaChapterFetcherTask periodicTask = new();
        GetMangaChaptersTask runOnceTask = new(Guid.NewGuid());
        TasksCollection.PeriodicTasks.Add(periodicTask);
        TasksCollection.RunOnceTasks.TryAdd(runOnceTask.TaskId, runOnceTask);

        TaskBase[] tasks = TasksCollection.GetKnownTasks().ToArray();

        Assert.Contains(periodicTask, tasks);
        Assert.Contains(runOnceTask, tasks);
        Assert.True(tasks.Length >= 2, $"Expected at least 2 tasks, but got {tasks.Length}");
    }
}

