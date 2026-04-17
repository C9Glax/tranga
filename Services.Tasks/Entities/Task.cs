using Services.Tasks.TaskTypes;

namespace Services.Tasks.Entities;

public abstract record Task
{
    public required Guid TaskId { get; init; }
    
    public required Guid TaskTypeId { get; init; }
    
    public required string TaskTypeName { get; init; }

    public required TaskType TaskType { get; init; }
}