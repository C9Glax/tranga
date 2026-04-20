namespace Services.Tasks.TaskTypes;

internal interface ITask
{
    public Guid TaskId { get; init; }
    
    public Guid TaskTypeId { get; init; }

    public TaskType TaskType { internal get; init; }
}