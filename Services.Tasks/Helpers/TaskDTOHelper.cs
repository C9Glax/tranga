using Services.Tasks.Entities;
using Services.Tasks.TaskTypes;
using PeriodicTask = Services.Tasks.Entities.PeriodicTask;

namespace Services.Tasks.Helpers;

internal static class TaskDTOHelper
{
    public static Entities.Task ToDTO(this ITask task) => task.TaskType switch
    {
        TaskType.PeriodicTask => new PeriodicTask()
        {
            TaskId = task.TaskId,
            TaskType = task.TaskType,
            TaskTypeName = task.GetType().Name,
            TaskTypeId = task.TaskTypeId,
            LastRun = (task as Services.Tasks.TaskTypes.PeriodicTask)!.LastRun,
            Interval = (task as Services.Tasks.TaskTypes.PeriodicTask)!.Interval
        },
        TaskType.RunOnceTask => new Entities.RunOnceTask()
        {
            TaskId = task.TaskId,
            TaskType = task.TaskType,
            TaskTypeName = task.GetType().Name,
            TaskTypeId = task.TaskTypeId,
        },
        _ => throw new NotImplementedException()
    };
    
    public static MangaTask ToDTO(this IMangaTask task) => task.TaskType switch
    {
        TaskType.PeriodicTask => new MangaPeriodicTask()
        {
            TaskId = task.TaskId,
            TaskType = task.TaskType,
            TaskTypeName = task.GetType().Name,
            TaskTypeId = task.TaskTypeId,
            LastRun = (task as Services.Tasks.TaskTypes.PeriodicTask)!.LastRun,
            Interval = (task as Services.Tasks.TaskTypes.PeriodicTask)!.Interval,
            MangaId = task.MangaId,
        },
        TaskType.RunOnceTask => new MangaRunOnceTask()
        {
            TaskId = task.TaskId,
            TaskType = task.TaskType,
            TaskTypeName = task.GetType().Name,
            TaskTypeId = task.TaskTypeId,
            MangaId = task.MangaId,
        },
        _ => throw new NotImplementedException()
    };
    
    public static ChapterTask ToDTO(this IChapterTask task) => task.TaskType switch
    {
        TaskType.PeriodicTask => new ChapterPeriodicTask()
        {
            TaskId = task.TaskId,
            TaskType = task.TaskType,
            TaskTypeName = task.GetType().Name,
            TaskTypeId = task.TaskTypeId,
            LastRun = (task as Services.Tasks.TaskTypes.PeriodicTask)!.LastRun,
            Interval = (task as Services.Tasks.TaskTypes.PeriodicTask)!.Interval,
            MangaId = task.MangaId,
            ChapterId = task.ChapterId,
        },
        TaskType.RunOnceTask => new ChapterRunOnceTask()
        {
            TaskId = task.TaskId,
            TaskType = task.TaskType,
            TaskTypeName = task.GetType().Name,
            TaskTypeId = task.TaskTypeId,
            MangaId = task.MangaId,
            ChapterId = task.ChapterId,
        },
        _ => throw new NotImplementedException()
    };
}