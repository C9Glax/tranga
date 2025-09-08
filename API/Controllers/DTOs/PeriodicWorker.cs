using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using API.Workers;

namespace API.Controllers.DTOs;

/// <summary>
/// <see cref="IPeriodic"/> DTO (<seealso cref="Worker"/> <seealso cref="BaseWorker"/>)
/// </summary>
public sealed record PeriodicWorker(string Key, IEnumerable<string> Dependencies, IEnumerable<string> MissingDependencies, bool DependenciesFulfilled, WorkerExecutionState State, DateTime LastExecution, TimeSpan Interval, DateTime NextExecution)
    : Worker(Key, Dependencies, MissingDependencies, DependenciesFulfilled, State)
{
    /// <summary>
    /// Timestamp when Worker executed last.
    /// </summary>
    [Required]
    [Description("Timestamp when Worker executed last.")]
    public DateTime LastExecution { get; init; } = LastExecution;
    
    /// <summary>
    /// Interval at which Worker runs.
    /// </summary>
    [Required]
    [Description("Interval at which Worker runs.")]
    public TimeSpan Interval { get; init; } = Interval;
    
    /// <summary>
    /// Timestamp when Worker is scheduled to execute next.
    /// </summary>
    [Required]
    [Description("Timestamp when Worker is scheduled to execute next.")]
    public DateTime NextExecution { get; init; } = LastExecution;
}