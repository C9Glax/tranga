namespace Services.Tasks.Database;

public sealed record DbRunOnceTask : DbTask
{
    public required bool HasRun { get; set; } = false;
}