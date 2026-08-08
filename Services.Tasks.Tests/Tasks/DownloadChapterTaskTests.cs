using Services.Tasks.Tasks;
using Services.Tasks.TaskTypes;
using Services.Tasks.WorkerLogic;

namespace Services.Tasks.Tests.Tasks;

public class DownloadChapterTaskTests
{
    [Fact]
    public void DependsOnTaskTypeIds_ContainsGetMangaChaptersTask()
    {
        DownloadChapterTask task = new(Guid.CreateVersion7(), Guid.CreateVersion7());

        Assert.Contains(GetMangaChaptersTask.TaskTypeIdValue, task.DependsOnTaskTypeIds);
    }

    [Fact]
    public void IsBlocked_WhileGetMangaChaptersTaskForSameMangaIsNonTerminal_ThenUnblockedOnceCompleted()
    {
        Guid mangaId = Guid.CreateVersion7();
        GetMangaChaptersTask fetch = new(mangaId);
        DownloadChapterTask download = new(mangaId, Guid.CreateVersion7());

        try
        {
            TasksCollection.RunOnceTasks.TryAdd(fetch.TaskId, fetch);
            TasksCollection.RunOnceTasks.TryAdd(download.TaskId, download);

            Assert.True(TaskDependencyResolver.IsBlocked(download));

            fetch.Status = TaskState.Completed;

            Assert.False(TaskDependencyResolver.IsBlocked(download));
        }
        finally
        {
            TasksCollection.RunOnceTasks.TryRemove(fetch.TaskId, out _);
            TasksCollection.RunOnceTasks.TryRemove(download.TaskId, out _);
        }
    }
}
