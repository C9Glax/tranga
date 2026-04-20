namespace Services.Tasks.TaskTypes;

internal interface IChapterTask : IMangaTask, ITask
{
    public Guid ChapterId { get; init; }
}