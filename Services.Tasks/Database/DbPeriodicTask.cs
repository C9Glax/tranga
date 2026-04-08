namespace Services.Tasks.Database;

public record DbPeriodicTask : DbTask
{
    public required DateTimeOffset LastRun { get; init; } = DateTimeOffset.MinValue;
}