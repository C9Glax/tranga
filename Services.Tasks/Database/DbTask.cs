using Services.Tasks.TaskTypes;

namespace Services.Tasks.Database;

public abstract record DbTask
{
    public Guid TaskId { get; init; } = Guid.CreateVersion7();
    
    public required Guid TaskTypeId { get; init; }
    
    public required TaskType TaskType { get; init; }
}