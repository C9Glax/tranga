namespace Services.Tasks;

internal abstract class TaskBase(TaskType t, Guid taskTypeId) : BackgroundService
{
    public Guid TaskTypeId { get; init; } = taskTypeId;

    internal readonly TaskType TaskType = t;
}

public enum TaskType : byte
{
    PeriodicTask = 0,
    RunOnceTask = 1
}