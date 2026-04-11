using Microsoft.EntityFrameworkCore;
using Services.Tasks.Tasks;
using Services.Tasks.TaskTypes;

namespace Services.Tasks.Database;

public static class Helpers
{
    
    internal static TaskType GetTaskType(this Guid taskTypeId) => TasksCollection.Tasks.FirstOrDefault(t => t.TaskTypeId == taskTypeId)?.TaskType ?? throw new NotImplementedException("Task not added to TasksCollection");
    
    internal static TaskType GetTaskType<T>() where T : TaskBase, new() => new T().TaskType;
    
    internal static async Task<T?> GetTask<T>(this IQueryable<DbTask> tasks, Guid taskId, CancellationToken ct) where T : DbTask =>
        await tasks.OfType<T>().FirstOrDefaultAsync(t => t.TaskId == taskId, ct);
    
    internal static IQueryable<T> FilterTasks<T>(this IQueryable<DbTask> tasks, Guid taskId) where T : DbTask =>
        tasks.OfType<T>().Where(t => t.TaskId == taskId);
    
    internal static IQueryable<T> FilterTasks<T>(this IQueryable<DbTask> tasks) where T : DbTask =>
        tasks.OfType<T>();
}