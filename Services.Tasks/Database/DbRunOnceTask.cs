namespace Services.Tasks.Database;

public sealed record DbRunOnceTask : DbTask
{
    public bool HasRun { get; set; } = false;
}