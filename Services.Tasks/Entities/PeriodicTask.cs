namespace Services.Tasks.Entities;

public record PeriodicTask : Task
{
    public required TimeSpan Interval { get; init; }
    
    public required DateTimeOffset LastRun { get; init; }
}