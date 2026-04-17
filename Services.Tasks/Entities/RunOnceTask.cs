namespace Services.Tasks.Entities;

public record RunOnceTask : Task
{
    public required bool HasRun { get; init; }
}