namespace Services.Tasks.Database;

public record DbPeriodicTask : DbTask
{
    public DateTimeOffset LastRun { get; init; } = DateTimeOffset.UnixEpoch;
}