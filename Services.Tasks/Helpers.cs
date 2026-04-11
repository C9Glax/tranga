using Services.Tasks.Database;
using Services.Tasks.Tasks;
using Services.Tasks.TaskTypes;

namespace Services.Tasks;

public static class Helpers
{
    public static Type GetTaskType(this DbTask dbTask) => TasksCollection.Tasks.FirstOrDefault(t => t.TaskTypeId == dbTask.TaskTypeId)?.GetType()  ?? throw new Exception($"Can't find Task Type {dbTask.TaskTypeId}!");

    internal static TimeSpan? GetTaskTimeout(this DbPeriodicTask task) =>
        TasksCollection.Tasks.OfType<PeriodicTask>().FirstOrDefault(t => t.TaskTypeId == task.TaskTypeId)?.Interval;
    
    internal static TaskBase CreateTaskFromDbTask(this DbTask dbTask) => (Activator.CreateInstance(dbTask.GetTaskType()) as TaskBase)!;

    internal static DbTask CreateDbTaskFromTask(this TaskBase task)
    {
        return task.TaskType switch
        {
            TaskType.PeriodicTask => new DbPeriodicTask()
            {
                TaskTypeId = task.TaskTypeId,
                TaskType = task.TaskType
            },
            TaskType.RunOnceTask => new DbRunOnceTask()
            {
                TaskTypeId = task.TaskTypeId,
                TaskType = task.TaskType
            },
            _ => throw new NotImplementedException()
        };
    }
}