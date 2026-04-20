using Services.Tasks.TaskTypes;

namespace Services.Tasks.Helpers;

internal static class TaskDTOHelper
{
    public static Entities.Task ToDTO(this TaskBase task) => task.TaskType switch
    {
        TaskType.PeriodicTask => (task as PeriodicTask)!.ToDTO(),
        TaskType.RunOnceTask => (task as RunOnceTask)!.ToDTO(),
        _ => throw new NotImplementedException()
    };

    public static Entities.PeriodicTask ToDTO(this PeriodicTask task) => new()
    {
        TaskId = task.TaskId,
        TaskType = task.TaskType,
        TaskTypeName = task.GetType().Name,
        TaskTypeId = task.TaskTypeId,
        LastRun = task.LastRun,
        Interval = task.Interval
    };
    
    public static Entities.RunOnceTask ToDTO(this RunOnceTask task) => new()
    {
        TaskId = task.TaskId,
        TaskType = task.TaskType,
        TaskTypeName = task.GetType().Name,
        TaskTypeId = task.TaskTypeId,
    };
}