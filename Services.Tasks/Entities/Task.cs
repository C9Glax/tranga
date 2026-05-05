using Services.Tasks.TaskTypes;

namespace Services.Tasks.Entities;

public sealed record Task
{
    /// <summary>
    /// Id of Task
    /// </summary>
    public required Guid TaskId { get; init; }
    
    /// <summary>
    /// Id of TaskType
    /// </summary>
    public required Guid TaskTypeId { get; init; }
    
    /// <summary>
    /// Name of TaskType
    /// </summary>
    public required string TaskTypeName { get; init; }

    /// <summary>
    /// TaskType
    /// </summary>
    public required TaskType TaskType { get; init; }
    
    /// <summary>
    /// Last run of Task (null if task has never run)
    /// </summary>
    public required DateTimeOffset? LastRun { get; init; }
    
    /// <summary>
    /// Id of Manga (if <see cref="IMangaTask"/>)
    /// </summary>
    public Guid? MangaId { get; init; }
    
    /// <summary>
    /// Id of Chapter (if <see cref="IChapterTask"/>)
    /// </summary>
    public Guid? ChapterId { get; init; }
    
    /// <summary>
    /// Interval of Task (if <see cref="PeriodicTask"/>)
    /// </summary>
    public TimeSpan? Interval { get; init; }
}