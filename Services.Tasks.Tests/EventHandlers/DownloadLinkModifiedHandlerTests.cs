using Moq;
using RabbitMQ.Client;
using Services.Tasks.EventHandlers;
using Services.Tasks.Tasks;
using Services.Tasks.TaskTypes;
using Services.Tasks.WorkerLogic;

namespace Services.Tasks.Tests.EventHandlers;

public class DownloadLinkModifiedHandlerTests
{
    [Fact]
    public void DownloadLinkModifiedHandler_CanBeInstantiated()
    {
        // Arrange
        Mock<IChannel> mockChannel = new ();
        Mock<IServiceProvider> mockServiceProvider = new ();

        // Act
        DownloadLinkModifiedHandler handler = new (mockChannel.Object, mockServiceProvider.Object);

        // Assert
        Assert.NotNull(handler);
    }

    [Fact]
    public void DownloadLinkModifiedHandler_IsEventHandler()
    {
        // Arrange
        Mock<IChannel> mockChannel = new ();
        Mock<IServiceProvider> mockServiceProvider = new ();

        // Act
        DownloadLinkModifiedHandler handler = new (mockChannel.Object, mockServiceProvider.Object);

        // Assert
        Assert.IsAssignableFrom<Common.Services.Events.IEventHandler>(handler);
    }

    [Fact]
    public void DownloadLinkModifiedHandler_AcceptsChannelAndServiceProvider()
    {
        // Arrange
        Mock<IChannel> mockChannel = new ();
        Mock<IServiceProvider> mockServiceProvider = new ();

        // Act & Assert - Constructor should not throw
        DownloadLinkModifiedHandler handler = new (mockChannel.Object, mockServiceProvider.Object);
        Assert.NotNull(handler);
    }
}

public class TaskCreationIntegrationTests : IDisposable
{
    public void Dispose()
    {
        TasksCollection.RunOnceTasks.Clear();
        TasksCollection.PeriodicTasks.Clear();
    }

    [Fact]
    public async Task GetMangaChaptersTask_CanBeCreatedAndQueued()
    {
        // Arrange
        TasksCollection.RunOnceTasks.Clear();
        int initialCount = TasksCollection.RunOnceTasks.Count;
        Guid mangaId = Guid.NewGuid();
        GetMangaChaptersTask task = new (mangaId);

        // Act
        TasksCollection.RunOnceTasks[task.TaskId] = task;

        // Assert
        Assert.Equal(initialCount + 1, TasksCollection.RunOnceTasks.Count);
        Assert.True(TasksCollection.RunOnceTasks.TryGetValue(task.TaskId, out RunOnceTask? retrievedTask));
        Assert.Same(task, retrievedTask);
        Assert.Equal(mangaId, ((GetMangaChaptersTask)retrievedTask).MangaId);
    }

    [Fact]
    public void GetMangaChaptersTask_HasCorrectTaskType()
    {
        // Arrange
        Guid mangaId = Guid.NewGuid();
        GetMangaChaptersTask task = new (mangaId);

        // Act & Assert
        Assert.Equal(TaskType.RunOnceTask, task.TaskType);
    }

    [Fact]
    public void GetMangaChaptersTask_ImplementsIMangaTask()
    {
        // Arrange
        Guid mangaId = Guid.NewGuid();
        GetMangaChaptersTask task = new (mangaId);

        // Act & Assert
        Assert.IsAssignableFrom<IMangaTask>(task);
        Assert.Equal(mangaId, task.MangaId);
    }

    [Fact]
    public void TasksCollection_CanRetrieveTaskByIdAfterAddition()
    {
        // Arrange
        TasksCollection.RunOnceTasks.Clear();
        TasksCollection.PeriodicTasks.Clear();

        Guid mangaId = Guid.NewGuid();
        GetMangaChaptersTask task = new (mangaId);
        TasksCollection.RunOnceTasks.TryAdd(task.TaskId, task);

        // Act
        TaskBase? retrieved = TasksCollection.GetTask(task.TaskId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(task.TaskId, retrieved.TaskId);
    }

    [Fact]
    public void TasksCollection_ReturnsNullForNonExistentTask()
    {
        // Arrange
        TasksCollection.RunOnceTasks.Clear();
        TasksCollection.PeriodicTasks.Clear();

        // Act
        TaskBase? retrieved = TasksCollection.GetTask(Guid.NewGuid());

        // Assert
        Assert.Null(retrieved);
    }
}

