using Services.Tasks.TaskTypes;

namespace Services.Tasks.WorkerLogic;

/// <summary>
/// Resolves whether a <see cref="TaskBase"/> is blocked by another currently-known Task of a type it
/// declares a dependency on (<see cref="TaskBase.DependsOnTaskTypeIds"/>), correlated by <see cref="IMangaTask.MangaId"/>.
/// </summary>
internal static class TaskDependencyResolver
{
    private static readonly TaskState[] NonTerminal =
        [TaskState.Pending, TaskState.Blocked, TaskState.Queued, TaskState.Running];

    /// <summary>
    /// True if <paramref name="candidate"/> declares a dependency on a Task type that has a known instance,
    /// for the same <see cref="IMangaTask.MangaId"/>, that has not yet reached a terminal <see cref="TaskState"/>.
    /// </summary>
    internal static bool IsBlocked(TaskBase candidate)
    {
        if (candidate.DependsOnTaskTypeIds.Count == 0 || candidate is not IMangaTask mangaTask)
            return false;

        return TasksCollection.GetKnownTasks().Any(other =>
            other.TaskId != candidate.TaskId &&
            candidate.DependsOnTaskTypeIds.Contains(other.TaskTypeId) &&
            other is IMangaTask otherManga && otherManga.MangaId == mangaTask.MangaId &&
            NonTerminal.Contains(other.Status));
    }
}
