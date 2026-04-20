using Services.Tasks.TaskTypes;

namespace Services.Tasks.Entities;

public interface IPeriodicTask
{
    public TimeSpan Interval { get; init; }
    
    public DateTimeOffset LastRun { get; init; }
}

public record PeriodicTask : Task, IPeriodicTask
{
    public required TimeSpan Interval { get; init; }
    
    public required DateTimeOffset LastRun { get; init; }
}

public record MangaPeriodicTask : MangaTask, IPeriodicTask
{
    public required TimeSpan Interval { get; init; }
    public required DateTimeOffset LastRun { get; init; }
}

public record ChapterPeriodicTask : ChapterTask, IPeriodicTask
{
    public required TimeSpan Interval { get; init; }
    public required DateTimeOffset LastRun { get; init; }
}