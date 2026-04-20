using Microsoft.AspNetCore.Http.HttpResults;
using Services.Tasks.Helpers;
using Services.Tasks.TaskTypes;
using Services.Tasks.WorkerLogic;

namespace Services.Tasks.Features.Tasks;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class GetTaskListEndpoint
{
    /// <summary>
    /// Get all Tasks
    /// </summary>
    /// <returns>List of all Tasks</returns>
    /// <response code="200">List of all Tasks</response>
    public static Ok<Entities.Task[]> Handle()
    {
        IEnumerable<TaskBase> knownTasks = TasksCollection.GetKnownTasks();

        Entities.Task[] result = knownTasks.Select(t => t.ToDTO()).ToArray();
        return TypedResults.Ok(result);
    }
}