using System.Collections.Concurrent;
using Services.Tasks.TaskTypes;

namespace Services.Tasks.WorkerLogic;

/// <summary>
/// Keeps track of all existing Tasks
/// </summary>
internal static class TasksCollection
{
    internal static readonly ConcurrentBag<PeriodicTask> PeriodicTasks = new();

    internal static readonly ConcurrentDictionary<Guid, RunOnceTask> RunOnceTasks = new();

    internal static IEnumerable<TaskBase> GetKnownTasks() => PeriodicTasks.Concat<TaskBase>(RunOnceTasks.Values);

    internal static TaskBase? GetTask(Guid taskId) => RunOnceTasks.TryGetValue(taskId, out RunOnceTask? task)
        ? task
        : PeriodicTasks.FirstOrDefault(t => t.TaskId == taskId);
}