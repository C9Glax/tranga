using Services.Tasks.Database;
using Services.Tasks.TaskTypes;
using PeriodicTask = Services.Tasks.Entities.PeriodicTask;
using RunOnceTask = Services.Tasks.Entities.RunOnceTask;

namespace Services.Tasks.Helpers;

internal static class TaskDTOHelper
{
    public static Entities.Task ToDTO(this DbTask task) => task.TaskType switch
    {
        TaskType.PeriodicTask => (task as DbPeriodicTask)!.ToDTO(),
        TaskType.RunOnceTask => (task as DbRunOnceTask)!.ToDTO(),
        _ => throw new NotImplementedException()
    };

    public static PeriodicTask ToDTO(this DbPeriodicTask task) => new()
    {
        TaskId = task.TaskId,
        TaskType = task.TaskType,
        TaskTypeName = task.GetTaskType().Name,
        TaskTypeId = task.TaskTypeId,
        LastRun = task.LastRun,
        Interval = task.GetTaskTimeout()
    };
    
    public static RunOnceTask ToDTO(this DbRunOnceTask task) => new()
    {
        TaskId = task.TaskId,
        TaskType = task.TaskType,
        TaskTypeName = task.GetTaskType().Name,
        TaskTypeId = task.TaskTypeId,
        HasRun = task.HasRun
    };
}