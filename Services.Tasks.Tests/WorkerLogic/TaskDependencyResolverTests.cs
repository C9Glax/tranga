using Services.Tasks.TaskTypes;
using Services.Tasks.Tests.Helpers;
using Services.Tasks.WorkerLogic;

namespace Services.Tasks.Tests.WorkerLogic;

public class TaskDependencyResolverTests
{
    [Fact]
    public void IsBlocked_ReturnsFalse_WhenNoDependenciesDeclared()
    {
        TestMangaRunOnceTask candidate = new(Guid.CreateVersion7());

        Assert.False(TaskDependencyResolver.IsBlocked(candidate));
    }

    [Theory]
    [InlineData(TaskState.Pending)]
    [InlineData(TaskState.Blocked)]
    [InlineData(TaskState.Queued)]
    [InlineData(TaskState.Running)]
    public void IsBlocked_ReturnsTrue_WhenDependencyOfSameMangaIdIsNonTerminal(TaskState dependencyStatus)
    {
        Guid mangaId = Guid.CreateVersion7();
        Guid dependencyTypeId = Guid.CreateVersion7();
        TestMangaRunOnceTask dependency = new(mangaId, dependencyTypeId) { Status = dependencyStatus };
        TestMangaRunOnceTask candidate = new(mangaId, dependsOn: [dependencyTypeId]);

        try
        {
            TasksCollection.RunOnceTasks.TryAdd(dependency.TaskId, dependency);
            TasksCollection.RunOnceTasks.TryAdd(candidate.TaskId, candidate);

            Assert.True(TaskDependencyResolver.IsBlocked(candidate));
        }
        finally
        {
            TasksCollection.RunOnceTasks.TryRemove(dependency.TaskId, out _);
            TasksCollection.RunOnceTasks.TryRemove(candidate.TaskId, out _);
        }
    }

    [Theory]
    [InlineData(TaskState.Completed)]
    [InlineData(TaskState.Failed)]
    public void IsBlocked_ReturnsFalse_WhenDependencyOfSameMangaIdIsTerminal(TaskState dependencyStatus)
    {
        Guid mangaId = Guid.CreateVersion7();
        Guid dependencyTypeId = Guid.CreateVersion7();
        TestMangaRunOnceTask dependency = new(mangaId, dependencyTypeId) { Status = dependencyStatus };
        TestMangaRunOnceTask candidate = new(mangaId, dependsOn: [dependencyTypeId]);

        try
        {
            TasksCollection.RunOnceTasks.TryAdd(dependency.TaskId, dependency);
            TasksCollection.RunOnceTasks.TryAdd(candidate.TaskId, candidate);

            Assert.False(TaskDependencyResolver.IsBlocked(candidate));
        }
        finally
        {
            TasksCollection.RunOnceTasks.TryRemove(dependency.TaskId, out _);
            TasksCollection.RunOnceTasks.TryRemove(candidate.TaskId, out _);
        }
    }

    [Fact]
    public void IsBlocked_ReturnsFalse_WhenDependencyExistsForDifferentMangaId()
    {
        Guid dependencyTypeId = Guid.CreateVersion7();
        TestMangaRunOnceTask dependency = new(Guid.CreateVersion7(), dependencyTypeId) { Status = TaskState.Running };
        TestMangaRunOnceTask candidate = new(Guid.CreateVersion7(), dependsOn: [dependencyTypeId]);

        try
        {
            TasksCollection.RunOnceTasks.TryAdd(dependency.TaskId, dependency);
            TasksCollection.RunOnceTasks.TryAdd(candidate.TaskId, candidate);

            Assert.False(TaskDependencyResolver.IsBlocked(candidate));
        }
        finally
        {
            TasksCollection.RunOnceTasks.TryRemove(dependency.TaskId, out _);
            TasksCollection.RunOnceTasks.TryRemove(candidate.TaskId, out _);
        }
    }

    [Fact]
    public void IsBlocked_ReturnsFalse_WhenNoDependencyInstanceExistsAtAll()
    {
        TestMangaRunOnceTask candidate = new(Guid.CreateVersion7(), dependsOn: [Guid.CreateVersion7()]);

        try
        {
            TasksCollection.RunOnceTasks.TryAdd(candidate.TaskId, candidate);

            Assert.False(TaskDependencyResolver.IsBlocked(candidate));
        }
        finally
        {
            TasksCollection.RunOnceTasks.TryRemove(candidate.TaskId, out _);
        }
    }
}
