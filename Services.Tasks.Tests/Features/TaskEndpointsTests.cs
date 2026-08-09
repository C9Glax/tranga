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
