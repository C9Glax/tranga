namespace Services.Tasks.TaskTypes;

internal interface IMangaTask : ITask
{
    public Guid MangaId { get; init; }
}