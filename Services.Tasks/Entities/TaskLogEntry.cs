namespace Services.Tasks.Entities;

/// <summary>
/// A single captured log line emitted while a Task executed.
/// </summary>
public sealed record TaskLogEntry
{
    /// <summary>When this log line was emitted.</summary>
    public required DateTimeOffset Timestamp { get; init; }
    /// <summary>Log level (e.g. "Debug", "Information", "Warning", "Error") of this line.</summary>
    public required string Level { get; init; }
    /// <summary>The logged message text.</summary>
    public required string Message { get; init; }
}
