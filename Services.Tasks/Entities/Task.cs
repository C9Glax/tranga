using System.Text.Json.Serialization;
using Services.Tasks.TaskTypes;

namespace Services.Tasks.Entities;

/// <summary>
/// A Task
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(Task), "task")]
[JsonDerivedType(typeof(MangaTask), "mangaTask")]
[JsonDerivedType(typeof(ChapterTask), "chapterTask")]
public record Task
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
    /// Current lifecycle state of the Task
    /// </summary>
    public required TaskState Status { get; init; }

    /// <summary>
    /// Interval of Task (if <see cref="PeriodicTask"/>)
    /// </summary>
    public TimeSpan? Interval { get; init; }
}

/// <summary>
/// A <see cref="Task"/> that relates to a Manga (see <see cref="IMangaTask"/>)
/// </summary>
public record MangaTask : Task
{
    /// <summary>
    /// Summary of the related Manga
    /// </summary>
    public required MangaSummary Manga { get; init; }
}

/// <summary>
/// A <see cref="Task"/> that relates to a Chapter (see <see cref="IChapterTask"/>)
/// </summary>
public sealed record ChapterTask : MangaTask
{
    /// <summary>
    /// Summary of the related Chapter
    /// </summary>
    public required ChapterSummary Chapter { get; init; }
}

/// <summary>
/// A minimal summary of a Manga, embedded in <see cref="MangaTask"/>
/// </summary>
public sealed record MangaSummary
{
    /// <summary>
    /// Id of Manga
    /// </summary>
    public required Guid MangaId { get; init; }

    /// <summary>
    /// Series title of the Manga's chosen metadata entry (null if unavailable)
    /// </summary>
    public string? Series { get; init; }
}

/// <summary>
/// A minimal summary of a Chapter, embedded in <see cref="ChapterTask"/>
/// </summary>
public sealed record ChapterSummary
{
    /// <summary>
    /// Id of Chapter
    /// </summary>
    public required Guid ChapterId { get; init; }

    /// <summary>
    /// Id of the Chapter's Manga
    /// </summary>
    public required Guid MangaId { get; init; }

    /// <summary>
    /// Title of Chapter
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Volume of Chapter
    /// </summary>
    public string? Volume { get; init; }

    /// <summary>
    /// Number of Chapter
    /// </summary>
    public required string Number { get; init; }
}
