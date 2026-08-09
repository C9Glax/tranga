namespace Services.Tasks.Entities;

/// <summary>
/// A single captured log line emitted while a Task executed.
/// </summary>
public sealed record TaskLogEntry
{
    public required DateTimeOffset Timestamp { get; init; }
    public required string Level { get; init; }
    public required string Message { get; init; }
}
