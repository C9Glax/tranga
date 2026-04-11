using Services.Tasks.TaskTypes;

namespace Services.Tasks.Tasks;

internal static class TasksCollection
{
    internal static readonly TaskBase[] Tasks =
    [
        new DbFileCleanupTask(Guid.Empty)
    ];
}