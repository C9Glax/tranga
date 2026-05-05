using Services.Tasks.TaskTypes;

namespace Services.Tasks.Helpers;

internal static class TaskDTOHelper
{
    public static Entities.Task ToDto(this ITask task)
    {
        Entities.Task t = new ()
        {
            TaskId = task.TaskId,
            TaskTypeId = task.TaskTypeId,
            TaskTypeName = task.GetType().Name,
            TaskType = default,
        };

        if (task is PeriodicTask p)
        {
            t = t with
            {
                TaskType = TaskType.PeriodicTask,
                Interval = p.Interval,
                LastRun = p.LastRun
            };
        }else if (task is RunOnceTask r)
        {
            t = t with
            {
                TaskType = TaskType.RunOnceTask,
            };
        }
        else throw new NotImplementedException();

        if (task is IChapterTask c)
        {
            t = t with
            {
                ChapterId = c.ChapterId,
                MangaId = c.MangaId
            };
        }else if (task is IMangaTask m)
        {
            t = t with
            {
                MangaId = m.MangaId
            };
        }

        return t;
    }
}