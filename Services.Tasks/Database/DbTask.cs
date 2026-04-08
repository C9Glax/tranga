namespace Services.Tasks.Database;

public abstract record DbTask
{
    public Guid TaskId { get; init; }
    
    public required Guid TaskTypeId { get; init; }
}