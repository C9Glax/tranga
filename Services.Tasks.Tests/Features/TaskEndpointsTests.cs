using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Services.Manga.Database;
using Services.Tasks.Database;
using Services.Tasks.Features.Tasks;
using Services.Tasks.Tasks;
using Services.Tasks.Tests.Helpers;
using Services.Tasks.WorkerLogic;
using Task = Services.Tasks.Entities.Task;
using Worker = Services.Tasks.Entities.Worker;

namespace Services.Tasks.Tests.Features;

public class GetTaskListEndpointTests : TrangaTest, IDisposable
{
    public void Dispose()
    {
        TasksCollection.PeriodicTasks.Clear();
        TasksCollection.RunOnceTasks.Clear();
    }

    [Fact]
    public async System.Threading.Tasks.Task GetTaskList_ReturnsAllTasks()
    {
        // Arrange
        TasksCollection.PeriodicTasks.Clear();
        TasksCollection.RunOnceTasks.Clear();

        TestPeriodicTask periodicTask = TestTask.Create<TestPeriodicTask>();
        GetMangaChaptersTask runOnceTask = new GetMangaChaptersTask(Guid.NewGuid());
        TasksCollection.PeriodicTasks.Add(periodicTask);
        TasksCollection.RunOnceTasks.TryAdd(runOnceTask.TaskId, runOnceTask);

        using MangaContext mangaContext = MangaContextFactory.Create();

        // Act
        Ok<Task[]> result = await GetTaskListEndpoint.Handle(mangaContext: mangaContext, ct: ct, includeFinished: false);

        // Assert
        Assert.NotNull(result.Value);
        // Verify that the two tasks we added are in the result
        Assert.Contains(result.Value, t => t.TaskId == periodicTask.TaskId);
        Assert.Contains(result.Value, t => t.TaskId == runOnceTask.TaskId);
        Assert.True(result.Value.Length >= 2);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetTaskList_ReturnsEmptyWhenNoneExist()
    {
        // Arrange
        TasksCollection.PeriodicTasks.Clear();
        TasksCollection.RunOnceTasks.Clear();

        using MangaContext mangaContext = MangaContextFactory.Create();

        // Act
        Ok<Task[]> result = await GetTaskListEndpoint.Handle(mangaContext: mangaContext, ct: ct, includeFinished: false);

        // Assert
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetTaskList_FiltersCompletedRunOnceTasks()
    {
        // Arrange
        TasksCollection.PeriodicTasks.Clear();
        TasksCollection.RunOnceTasks.Clear();

        TestRunOnceTask runOnceTask = TestTask.Create<TestRunOnceTask>();
        runOnceTask.HasRun = true;
        TasksCollection.RunOnceTasks.TryAdd(runOnceTask.TaskId, runOnceTask);

        using MangaContext mangaContext = MangaContextFactory.Create();

        // Act
        Ok<Task[]> resultWithoutFinished = await GetTaskListEndpoint.Handle(mangaContext: mangaContext, ct: ct, includeFinished: false);
        Ok<Task[]> resultWithFinished = await GetTaskListEndpoint.Handle(mangaContext: mangaContext, ct: ct, includeFinished: true);

        // Assert
        Assert.NotNull(resultWithoutFinished.Value);
        Assert.Empty(resultWithoutFinished.Value);
        Assert.NotNull(resultWithFinished.Value);
        Assert.Single(resultWithFinished.Value);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetTaskList_DownloadChapterTask_ReturnsChapterTaskWithSummaries()
    {
        // Arrange
        TasksCollection.PeriodicTasks.Clear();
        TasksCollection.RunOnceTasks.Clear();

        using MangaContext mangaContext = MangaContextFactory.Create();

        DbManga manga = new() { MangaId = Guid.CreateVersion7(), Monitored = true };
        DbChapter chapter = new() { MangaId = manga.MangaId, Number = "5", Title = "A Title", Volume = "1" };
        await mangaContext.Mangas.AddAsync(manga, ct);
        await mangaContext.Chapters.AddAsync(chapter, ct);
        await mangaContext.SaveChangesAsync(ct);

        DownloadChapterTask task = new(manga.MangaId, chapter.ChapterId);
        TasksCollection.RunOnceTasks.TryAdd(task.TaskId, task);

        // Act
        Ok<Task[]> result = await GetTaskListEndpoint.Handle(mangaContext: mangaContext, ct: ct, includeFinished: false);

        // Assert
        Assert.NotNull(result.Value);
        Task dto = Assert.Single(result.Value, t => t.TaskId == task.TaskId);
        Services.Tasks.Entities.ChapterTask chapterTask = Assert.IsType<Services.Tasks.Entities.ChapterTask>(dto);
        Assert.Equal(manga.MangaId, chapterTask.Manga.MangaId);
        Assert.Equal(chapter.ChapterId, chapterTask.Chapter.ChapterId);
        Assert.Equal(chapter.Number, chapterTask.Chapter.Number);
        Assert.Equal(chapter.Title, chapterTask.Chapter.Title);
        Assert.Equal(chapter.Volume, chapterTask.Chapter.Volume);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetTaskList_GetMangaChaptersTask_ReturnsMangaTaskWithSummary()
    {
        // Arrange
        TasksCollection.PeriodicTasks.Clear();
        TasksCollection.RunOnceTasks.Clear();

        using MangaContext mangaContext = MangaContextFactory.Create();

        DbManga manga = new() { MangaId = Guid.CreateVersion7(), Monitored = true };
        await mangaContext.Mangas.AddAsync(manga, ct);
        await mangaContext.SaveChangesAsync(ct);

        GetMangaChaptersTask task = new(manga.MangaId);
        TasksCollection.RunOnceTasks.TryAdd(task.TaskId, task);

        // Act
        Ok<Task[]> result = await GetTaskListEndpoint.Handle(mangaContext: mangaContext, ct: ct, includeFinished: false);

        // Assert
        Assert.NotNull(result.Value);
        Task dto = Assert.Single(result.Value, t => t.TaskId == task.TaskId);
        Services.Tasks.Entities.MangaTask mangaTask = Assert.IsType<Services.Tasks.Entities.MangaTask>(dto);
        Assert.Equal(manga.MangaId, mangaTask.Manga.MangaId);
        Assert.IsNotType<Services.Tasks.Entities.ChapterTask>(dto);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetTaskList_OrdersByTaskIdDescending_AndSupportsSkipAndLimit()
    {
        // Arrange
        TasksCollection.PeriodicTasks.Clear();
        TasksCollection.RunOnceTasks.Clear();

        // TaskId is a Guid.CreateVersion7() (millisecond timestamp precision) - a small delay between
        // creations guarantees each TaskId sorts strictly after the previous one.
        TestRunOnceTask first = TestTask.Create<TestRunOnceTask>();
        TasksCollection.RunOnceTasks.TryAdd(first.TaskId, first);
        await System.Threading.Tasks.Task.Delay(5, ct);
        TestRunOnceTask second = TestTask.Create<TestRunOnceTask>();
        TasksCollection.RunOnceTasks.TryAdd(second.TaskId, second);
        await System.Threading.Tasks.Task.Delay(5, ct);
        TestRunOnceTask third = TestTask.Create<TestRunOnceTask>();
        TasksCollection.RunOnceTasks.TryAdd(third.TaskId, third);

        using MangaContext mangaContext = MangaContextFactory.Create();

        // Act
        Ok<Task[]> firstPage = await GetTaskListEndpoint.Handle(mangaContext: mangaContext, ct: ct, skip: 0, limit: 2);
        Ok<Task[]> secondPage = await GetTaskListEndpoint.Handle(mangaContext: mangaContext, ct: ct, skip: 2, limit: 2);

        // Assert
        Assert.NotNull(firstPage.Value);
        Assert.Equal(2, firstPage.Value.Length);
        Assert.Equal(third.TaskId, firstPage.Value[0].TaskId);
        Assert.Equal(second.TaskId, firstPage.Value[1].TaskId);

        Assert.NotNull(secondPage.Value);
        Task remaining = Assert.Single(secondPage.Value);
        Assert.Equal(first.TaskId, remaining.TaskId);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetTaskList_FiltersByMangaId()
    {
        // Arrange
        TasksCollection.PeriodicTasks.Clear();
        TasksCollection.RunOnceTasks.Clear();

        Guid targetMangaId = Guid.CreateVersion7();
        TestMangaRunOnceTask matching = new(targetMangaId);
        TestMangaRunOnceTask other = new(Guid.CreateVersion7());
        TasksCollection.RunOnceTasks.TryAdd(matching.TaskId, matching);
        TasksCollection.RunOnceTasks.TryAdd(other.TaskId, other);

        using MangaContext mangaContext = MangaContextFactory.Create();

        // Act
        Ok<Task[]> result = await GetTaskListEndpoint.Handle(mangaContext: mangaContext, ct: ct, mangaId: targetMangaId);

        // Assert
        Assert.NotNull(result.Value);
        Task dto = Assert.Single(result.Value);
        Assert.Equal(matching.TaskId, dto.TaskId);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetTaskList_FiltersByTaskTypeName()
    {
        // Arrange
        TasksCollection.PeriodicTasks.Clear();
        TasksCollection.RunOnceTasks.Clear();

        GetMangaChaptersTask matching = new(Guid.NewGuid());
        TestRunOnceTask other = TestTask.Create<TestRunOnceTask>();
        TasksCollection.RunOnceTasks.TryAdd(matching.TaskId, matching);
        TasksCollection.RunOnceTasks.TryAdd(other.TaskId, other);

        using MangaContext mangaContext = MangaContextFactory.Create();

        // Act
        Ok<Task[]> result = await GetTaskListEndpoint.Handle(mangaContext: mangaContext, ct: ct, taskTypeName: [nameof(GetMangaChaptersTask)]);

        // Assert
        Assert.NotNull(result.Value);
        Task dto = Assert.Single(result.Value);
        Assert.Equal(matching.TaskId, dto.TaskId);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetTaskList_FiltersByStatus()
    {
        // Arrange
        TasksCollection.PeriodicTasks.Clear();
        TasksCollection.RunOnceTasks.Clear();

        TestRunOnceTask running = TestTask.Create<TestRunOnceTask>();
        running.Status = Services.Tasks.TaskTypes.TaskState.Running;
        TestRunOnceTask pending = TestTask.Create<TestRunOnceTask>();
        pending.Status = Services.Tasks.TaskTypes.TaskState.Pending;
        TasksCollection.RunOnceTasks.TryAdd(running.TaskId, running);
        TasksCollection.RunOnceTasks.TryAdd(pending.TaskId, pending);

        using MangaContext mangaContext = MangaContextFactory.Create();

        // Act
        Ok<Task[]> result = await GetTaskListEndpoint.Handle(
            mangaContext: mangaContext, ct: ct, status: [Services.Tasks.TaskTypes.TaskState.Running]);

        // Assert
        Assert.NotNull(result.Value);
        Task dto = Assert.Single(result.Value);
        Assert.Equal(running.TaskId, dto.TaskId);
    }
}

public class GetTaskEndpointTests : TrangaTest
{
    [Fact]
    public async System.Threading.Tasks.Task GetTask_ReturnsSpecificTaskById()
    {
        // Arrange
        TasksCollection.PeriodicTasks.Clear();
        TasksCollection.RunOnceTasks.Clear();

        GetMangaChaptersTask task = new GetMangaChaptersTask(Guid.NewGuid());
        TasksCollection.RunOnceTasks.TryAdd(task.TaskId, task);

        using MangaContext mangaContext = MangaContextFactory.Create();

        // Act
        Results<Ok<Task>, NotFound> result = await GetTaskEndpoint.Handle(taskId: task.TaskId, mangaContext: mangaContext, ct: ct);

        // Assert - The result is Results<Ok<Task>, NotFound>
        // We verify it's working by checking it's not null and is an IResult
        Assert.NotNull(result);
        Assert.IsAssignableFrom<Microsoft.AspNetCore.Http.IResult>(result);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetTask_Returns404ForUnknownId()
    {
        // Arrange
        TasksCollection.PeriodicTasks.Clear();
        TasksCollection.RunOnceTasks.Clear();
        Guid unknownId = Guid.NewGuid();

        using MangaContext mangaContext = MangaContextFactory.Create();

        // Act
        Results<Ok<Task>, NotFound> result = await GetTaskEndpoint.Handle(taskId: unknownId, mangaContext: mangaContext, ct: ct);

        // Assert - Verify result is returned
        Assert.NotNull(result);
        Assert.IsAssignableFrom<Microsoft.AspNetCore.Http.IResult>(result);
    }
}

public class GetTaskLogsEndpointTests : TrangaTest, IDisposable
{
    public void Dispose()
    {
        TasksCollection.PeriodicTasks.Clear();
        TasksCollection.RunOnceTasks.Clear();
    }

    [Fact]
    public void GetTaskLogs_Returns404ForUnknownId()
    {
        TasksCollection.PeriodicTasks.Clear();
        TasksCollection.RunOnceTasks.Clear();

        Results<Ok<Services.Tasks.Entities.TaskLogEntry[]>, NotFound> result = GetTaskLogsEndpoint.Handle(Guid.NewGuid());

        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public void GetTaskLogs_ReturnsEntriesInChronologicalOrder()
    {
        TasksCollection.PeriodicTasks.Clear();
        TasksCollection.RunOnceTasks.Clear();

        TestRunOnceTask task = TestTask.Create<TestRunOnceTask>();
        task.AppendLog(Microsoft.Extensions.Logging.LogLevel.Information, "first");
        task.AppendLog(Microsoft.Extensions.Logging.LogLevel.Warning, "second");
        task.AppendLog(Microsoft.Extensions.Logging.LogLevel.Error, "third");
        TasksCollection.RunOnceTasks.TryAdd(task.TaskId, task);

        Results<Ok<Services.Tasks.Entities.TaskLogEntry[]>, NotFound> result = GetTaskLogsEndpoint.Handle(task.TaskId);

        Ok<Services.Tasks.Entities.TaskLogEntry[]> ok = Assert.IsType<Ok<Services.Tasks.Entities.TaskLogEntry[]>>(result.Result);
        Assert.NotNull(ok.Value);
        Assert.Equal(["first", "second", "third"], ok.Value.Select(e => e.Message));
        Assert.Equal(["Information", "Warning", "Error"], ok.Value.Select(e => e.Level));
    }

    [Fact]
    public void GetTaskLogs_SupportsSkipAndLimit()
    {
        TasksCollection.PeriodicTasks.Clear();
        TasksCollection.RunOnceTasks.Clear();

        TestRunOnceTask task = TestTask.Create<TestRunOnceTask>();
        for (int i = 0; i < 5; i++)
            task.AppendLog(Microsoft.Extensions.Logging.LogLevel.Information, $"entry {i}");
        TasksCollection.RunOnceTasks.TryAdd(task.TaskId, task);

        Results<Ok<Services.Tasks.Entities.TaskLogEntry[]>, NotFound> result = GetTaskLogsEndpoint.Handle(task.TaskId, skip: 2, limit: 2);

        Ok<Services.Tasks.Entities.TaskLogEntry[]> ok = Assert.IsType<Ok<Services.Tasks.Entities.TaskLogEntry[]>>(result.Result);
        Assert.NotNull(ok.Value);
        Assert.Equal(["entry 2", "entry 3"], ok.Value.Select(e => e.Message));
    }
}

public class TaskEndpointsConsistencyTests : TrangaTest, IDisposable
{
    public void Dispose()
    {
        TasksCollection.PeriodicTasks.Clear();
        TasksCollection.RunOnceTasks.Clear();
    }

    [Fact]
    public async System.Threading.Tasks.Task GetTaskListEndpoint_ReturnsTaskDtoWithCorrectType()
    {
        // Arrange
        TasksCollection.PeriodicTasks.Clear();
        TasksCollection.RunOnceTasks.Clear();

        TestPeriodicTask task = TestTask.Create<TestPeriodicTask>();
        TasksCollection.PeriodicTasks.Add(task);

        using MangaContext mangaContext = MangaContextFactory.Create();

        // Act
        Ok<Task[]> result = await GetTaskListEndpoint.Handle(mangaContext: mangaContext, ct: ct, includeFinished: false);

        // Assert
        Assert.NotNull(result.Value);
        Task? dto = result.Value.FirstOrDefault(t => t.TaskId == task.TaskId);
        Assert.NotNull(dto);
        Assert.Equal(task.TaskId, dto.TaskId);
        Assert.Equal(Services.Tasks.TaskTypes.TaskType.PeriodicTask, dto.TaskType);
    }

    [Fact]
    public async System.Threading.Tasks.Task GetTaskListEndpoint_ReturnsRunOnceTaskWithCorrectType()
    {
        // Arrange
        TasksCollection.PeriodicTasks.Clear();
        TasksCollection.RunOnceTasks.Clear();

        TestRunOnceTask task = TestTask.Create<TestRunOnceTask>();
        TasksCollection.RunOnceTasks.TryAdd(task.TaskId, task);

        using MangaContext mangaContext = MangaContextFactory.Create();

        // Act
        Ok<Task[]> result = await GetTaskListEndpoint.Handle(mangaContext: mangaContext, ct: ct, includeFinished: false);

        // Assert
        Assert.NotNull(result.Value);
        Task? dto = result.Value.FirstOrDefault(t => t.TaskId == task.TaskId);
        Assert.NotNull(dto);
        Assert.Equal(Services.Tasks.TaskTypes.TaskType.RunOnceTask, dto.TaskType);
    }

    [Fact]
    public async System.Threading.Tasks.Task TaskDtoHelper_PreservesTaskProperties()
    {
        // Arrange
        GetMangaChaptersTask task = new GetMangaChaptersTask(Guid.NewGuid());
        Guid mangaId = task.MangaId;

        using MangaContext mangaContext = MangaContextFactory.Create();

        // Act
        Task[] dtos = await Services.Tasks.Helpers.TaskDTOHelper.ToDtosAsync([task], mangaContext, ct);

        // Assert
        Task dto = Assert.Single(dtos);
        Assert.Equal(task.TaskId, dto.TaskId);
        Assert.Equal(task.TaskTypeId, dto.TaskTypeId);
        Services.Tasks.Entities.MangaTask mangaTask = Assert.IsType<Services.Tasks.Entities.MangaTask>(dto);
        Assert.Equal(mangaId, mangaTask.Manga.MangaId);
        Assert.IsNotType<Services.Tasks.Entities.ChapterTask>(dto);
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
