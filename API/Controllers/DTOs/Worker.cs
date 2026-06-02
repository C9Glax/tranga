using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using API.Workers;

namespace API.Controllers.DTOs;

/// <summary>
/// <see cref="BaseWorker"/> DTO
/// </summary>
public record Worker(string Key, IEnumerable<string> Dependencies, IEnumerable<string> MissingDependencies, bool DependenciesFulfilled, WorkerExecutionState State, float? Progress, int? CurrentStep, int? TotalSteps, string? ProgressDescription) : Identifiable(Key)
{
    /// <summary>
    /// Workers this worker depends on having ran.
    /// </summary>
    [Required]
    [Description("Workers this worker depends on having ran.")]
    public IEnumerable<string> Dependencies { get; init; } = Dependencies;

    /// <summary>
    /// Workers that have not yet ran, that need to run for this Worker to run.
    /// </summary>
    [Required]
    [Description("Workers that have not yet ran, that need to run for this Worker to run.")]
    public IEnumerable<string> MissingDependencies { get; init; } = MissingDependencies;

    /// <summary>
    /// Worker can run.
    /// </summary>
    [Required]
    [Description("Worker can run.")]
    public bool DependenciesFulfilled { get; init; } = DependenciesFulfilled;

    /// <summary>
    /// Execution state of the Worker.
    /// </summary>
    [Required]
    [Description("Execution state of the Worker.")]
    public WorkerExecutionState State { get; init; } = State;

    /// <summary>
    /// Overall progress (0.0 to 1.0).
    /// </summary>
    [Description("Overall progress (0.0 to 1.0).")]
    public float? Progress { get; init; } = Progress;

    /// <summary>
    /// Current step number (e.g. images downloaded so far).
    /// </summary>
    [Description("Current step number (e.g. images downloaded so far).")]
    public int? CurrentStep { get; init; } = CurrentStep;

    /// <summary>
    /// Total steps expected (e.g. total images in chapter).
    /// </summary>
    [Description("Total steps expected (e.g. total images in chapter).")]
    public int? TotalSteps { get; init; } = TotalSteps;

    /// <summary>
    /// Human-readable progress description.
    /// </summary>
    [Description("Human-readable progress description.")]
    public string? ProgressDescription { get; init; } = ProgressDescription;
}