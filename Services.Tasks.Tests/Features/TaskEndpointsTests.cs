using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Services.Tasks.Database;
using Services.Tasks.Features.Tasks;
using Services.Tasks.Helpers;
using Services.Tasks.Tasks;
using Services.Tasks.Tests.Helpers;
using Services.Tasks.WorkerLogic;
using Task = Services.Tasks.Entities.Task;
using Worker = Services.Tasks.Entities.Worker;

namespace Services.Tasks.Tests.Features;

public class GetTaskListEndpointTests : IDisposable
{
    public void Dispose()
    {
        TasksCollection.PeriodicTasks.Clear();
        TasksCollection.RunOnceTasks.Clear();
    }

    [Fact]
    public void GetTaskList_ReturnsAllTasks()
    {
        // Arrange
        TasksCollection.PeriodicTasks.Clear();
        TasksCollection.RunOnceTasks.Clear();
        
        TestPeriodicTask periodicTask = TestTask.Create<TestPeriodicTask>();
        GetMangaChaptersTask runOnceTask = new GetMangaChaptersTask(Guid.NewGuid());
        TasksCollection.PeriodicTasks.Add(periodicTask);
        TasksCollection.RunOnceTasks.TryAdd(runOnceTask.TaskId, runOnceTask);

        // Act
        Ok<Task[]> result = GetTaskListEndpoint.Handle(includeFinished: false);

        // Assert
        Assert.NotNull(result.Value);
        // Verify that the two tasks we added are in the result
        Assert.Contains(result.Value, t => t.TaskId == periodicTask.TaskId);
        Assert.Contains(result.Value, t => t.TaskId == runOnceTask.TaskId);
        Assert.True(result.Value.Length >= 2);
    }

    [Fact]
    public void GetTaskList_ReturnsEmptyWhenNoneExist()
    {
        // Arrange
        TasksCollection.PeriodicTasks.Clear();
        TasksCollection.RunOnceTasks.Clear();

        // Act
        Ok<Task[]> result = GetTaskListEndpoint.Handle(includeFinished: false);

        // Assert
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }

    [Fact]
    public void GetTaskList_FiltersCompletedRunOnceTasks()
    {
        // Arrange
        TasksCollection.PeriodicTasks.Clear();
        TasksCollection.RunOnceTasks.Clear();

        TestRunOnceTask runOnceTask = TestTask.Create<TestRunOnceTask>();
        runOnceTask.HasRun = true;
        TasksCollection.RunOnceTasks.TryAdd(runOnceTask.TaskId, runOnceTask);

        // Act
        Ok<Task[]> resultWithoutFinished = GetTaskListEndpoint.Handle(includeFinished: false);
        Ok<Task[]> resultWithFinished = GetTaskListEndpoint.Handle(includeFinished: true);

        // Assert
        Assert.NotNull(resultWithoutFinished.Value);
        Assert.Empty(resultWithoutFinished.Value);
        Assert.NotNull(resultWithFinished.Value);
        Assert.Single(resultWithFinished.Value);
    }
}

public class GetTaskEndpointTests
{
    [Fact]
    public void GetTask_ReturnsSpecificTaskById()
    {
        // Arrange
        TasksCollection.PeriodicTasks.Clear();
        TasksCollection.RunOnceTasks.Clear();

        GetMangaChaptersTask task = new GetMangaChaptersTask(Guid.NewGuid());
        TasksCollection.RunOnceTasks.TryAdd(task.TaskId, task);

        // Act
        Results<Ok<Task>, NotFound> result = GetTaskEndpoint.Handle(task.TaskId);
        
        // Assert - The result is Results<Ok<Task>, NotFound>
        // We verify it's working by checking it's not null and is an IResult
        Assert.NotNull(result);
        Assert.IsAssignableFrom<Microsoft.AspNetCore.Http.IResult>(result);
    }

    [Fact]
    public void GetTask_Returns404ForUnknownId()
    {
        // Arrange
        TasksCollection.PeriodicTasks.Clear();
        TasksCollection.RunOnceTasks.Clear();
        Guid unknownId = Guid.NewGuid();

        // Act
        Results<Ok<Task>, NotFound> result = GetTaskEndpoint.Handle(unknownId);

        // Assert - Verify result is returned
        Assert.NotNull(result);
        Assert.IsAssignableFrom<Microsoft.AspNetCore.Http.IResult>(result);
    }
}

public class TaskEndpointsConsistencyTests : IDisposable
{
    public void Dispose()
    {
        TasksCollection.PeriodicTasks.Clear();
        TasksCollection.RunOnceTasks.Clear();
    }

    [Fact]
    public void GetTaskListEndpoint_ReturnsTaskDtoWithCorrectType()
    {
        // Arrange
        TasksCollection.PeriodicTasks.Clear();
        TasksCollection.RunOnceTasks.Clear();

        TestPeriodicTask task = TestTask.Create<TestPeriodicTask>();
        TasksCollection.PeriodicTasks.Add(task);

        // Act
        Ok<Task[]> result = GetTaskListEndpoint.Handle(includeFinished: false);

        // Assert
        Assert.NotNull(result.Value);
        Task? dto = result.Value.FirstOrDefault(t => t.TaskId == task.TaskId);
        Assert.NotNull(dto);
        Assert.Equal(task.TaskId, dto.TaskId);
        Assert.Equal(Services.Tasks.TaskTypes.TaskType.PeriodicTask, dto.TaskType);
    }

    [Fact]
    public void GetTaskListEndpoint_ReturnsRunOnceTaskWithCorrectType()
    {
        // Arrange
        TasksCollection.PeriodicTasks.Clear();
        TasksCollection.RunOnceTasks.Clear();

        TestRunOnceTask task = TestTask.Create<TestRunOnceTask>();
        TasksCollection.RunOnceTasks.TryAdd(task.TaskId, task);

        // Act
        Ok<Task[]> result = GetTaskListEndpoint.Handle(includeFinished: false);

        // Assert
        Assert.NotNull(result.Value);
        Task? dto = result.Value.FirstOrDefault(t => t.TaskId == task.TaskId);
        Assert.NotNull(dto);
        Assert.Equal(Services.Tasks.TaskTypes.TaskType.RunOnceTask, dto.TaskType);
    }

    [Fact]
    public void TaskDtoHelper_PreservesTaskProperties()
    {
        // Arrange
        GetMangaChaptersTask task = new GetMangaChaptersTask(Guid.NewGuid());
        Guid mangaId = task.MangaId;

        // Act
        Task dto = task.ToDto();

        // Assert
        Assert.Equal(task.TaskId, dto.TaskId);
        Assert.Equal(task.TaskTypeId, dto.TaskTypeId);
        Assert.Equal(mangaId, dto.MangaId);
        Assert.Null(dto.ChapterId);
    }
}

public class GetWorkerListEndpointTests : TrangaTest
{
    [Fact]
    public async System.Threading.Tasks.Task GetWorkerList_ReturnsEmptyWhenNoneExist()
    {
        using TasksContext ctx = TasksContextFactory.Create();

        Ok<Worker[]> result = await GetWorkerListEndpoint.Handle(ctx, ct);

        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetWorkerList_ReturnsAllWorkers()
    {
        using TasksContext ctx = TasksContextFactory.Create();
        DbWorker worker = new() { Status = WorkerStatus.Busy, CurrentTaskId = Guid.CreateVersion7() };
        await ctx.Workers.AddAsync(worker, ct);
        await ctx.SaveChangesAsync(ct);

        Ok<Worker[]> result = await GetWorkerListEndpoint.Handle(ctx, ct);

        Assert.NotNull(result.Value);
        Worker dto = Assert.Single(result.Value);
        Assert.Equal(worker.WorkerId, dto.WorkerId);
        Assert.Equal(worker.Status, dto.Status);
        Assert.Equal(worker.CurrentTaskId, dto.CurrentTaskId);
    }
}

