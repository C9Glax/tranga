using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Tasks.Entities;
using Services.Tasks.TaskTypes;
using Task = Services.Tasks.Entities.Task;

namespace Services.Tasks.Helpers;

internal static class TaskDTOHelper
{
    public static async Task<Task[]> ToDtosAsync(this IEnumerable<ITask> tasks, MangaContext mangaContext, CancellationToken ct)
    {
        ITask[] taskArray = tasks.ToArray();
        Guid[] mangaIds = taskArray.OfType<IMangaTask>().Select(m => m.MangaId).Distinct().ToArray();
        Guid[] chapterIds = taskArray.OfType<IChapterTask>().Select(c => c.ChapterId).Distinct().ToArray();

        Dictionary<Guid, MangaSummary> mangaSummaries = await mangaContext.Mangas
            .Where(m => mangaIds.Contains(m.MangaId))
            .Select(m => new MangaSummary
            {
                MangaId = m.MangaId,
                Series = m.MetadataEntries!.Where(me => me.Chosen).Select(me => me.Metadata.Series).FirstOrDefault()
            })
            .ToDictionaryAsync(m => m.MangaId, ct);

        Dictionary<Guid, ChapterSummary> chapterSummaries = await mangaContext.Chapters
            .Where(c => chapterIds.Contains(c.ChapterId))
            .Select(c => new ChapterSummary
            {
                ChapterId = c.ChapterId,
                MangaId = c.MangaId,
                Title = c.Title,
                Volume = c.Volume,
                Number = c.Number
            })
            .ToDictionaryAsync(c => c.ChapterId, ct);

        return taskArray.Select(t => t.ToDto(mangaSummaries, chapterSummaries)).ToArray();
    }

    private static Task ToDto(this ITask task, IReadOnlyDictionary<Guid, MangaSummary> mangaSummaries, IReadOnlyDictionary<Guid, ChapterSummary> chapterSummaries)
    {
        Guid taskId = task.TaskId;
        Guid taskTypeId = task.TaskTypeId;
        string taskTypeName = task.GetType().Name;
        DateTimeOffset? lastRun = task.LastRun;
        TaskState status = task.Status;

        TaskType taskType;
        TimeSpan? interval = null;
        if (task is PeriodicTask p)
        {
            taskType = TaskType.PeriodicTask;
            interval = p.Interval;
        }
        else if (task is RunOnceTask)
        {
            taskType = TaskType.RunOnceTask;
        }
        else throw new NotImplementedException();

        if (task is IChapterTask c)
        {
            return new ChapterTask
            {
                TaskId = taskId,
                TaskTypeId = taskTypeId,
                TaskTypeName = taskTypeName,
                TaskType = taskType,
                LastRun = lastRun,
                Status = status,
                Interval = interval,
                Manga = mangaSummaries.GetValueOrDefault(c.MangaId) ?? new MangaSummary { MangaId = c.MangaId },
                Chapter = chapterSummaries.GetValueOrDefault(c.ChapterId) ?? new ChapterSummary { ChapterId = c.ChapterId, MangaId = c.MangaId, Number = "" }
            };
        }

        if (task is IMangaTask m)
        {
            return new MangaTask
            {
                TaskId = taskId,
                TaskTypeId = taskTypeId,
                TaskTypeName = taskTypeName,
                TaskType = taskType,
                LastRun = lastRun,
                Status = status,
                Interval = interval,
                Manga = mangaSummaries.GetValueOrDefault(m.MangaId) ?? new MangaSummary { MangaId = m.MangaId }
            };
        }

        return new Task
        {
            TaskId = taskId,
            TaskTypeId = taskTypeId,
            TaskTypeName = taskTypeName,
            TaskType = taskType,
            LastRun = lastRun,
            Status = status,
            Interval = interval
        };
    }
}
