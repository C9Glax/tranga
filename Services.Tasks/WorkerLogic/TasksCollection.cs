using System.Collections.Concurrent;
using Services.Tasks.TaskTypes;

namespace Services.Tasks.WorkerLogic;

internal static class TasksCollection
{
    internal static ConcurrentBag<PeriodicTask> PeriodicTasks = new();
    
    internal static ConcurrentDictionary<Guid, RunOnceTask> RunOnceTasks = new();

    internal static IEnumerable<TaskBase> GetKnownTasks() => PeriodicTasks.Concat<TaskBase>(RunOnceTasks.Values);

    internal static TaskBase? GetTask(Guid taskId) => RunOnceTasks.TryGetValue(taskId, out RunOnceTask? task)
        ? task
        : PeriodicTasks.FirstOrDefault(t => t.TaskId == taskId);
}