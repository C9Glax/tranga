using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Services.Tasks.Helpers;
using Services.Tasks.WorkerLogic;
using Task = Services.Tasks.Entities.Task;

namespace Services.Tasks.Features.Tasks;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
public abstract class GetTaskEndpoint
{
    /// <summary>
    /// Get Task
    /// </summary>
    /// <param name="taskId">ID of Task</param>
    /// <returns>Task</returns>
    /// <response code="200">Task</response>
    /// <response code="404">Task with requested ID does not exist</response>
    public static Results<Ok<Task>, NotFound> Handle([FromRoute] Guid taskId)
    {
        if (TasksCollection.GetTask(taskId) is not { } task)
            return TypedResults.NotFound();

        return TypedResults.Ok(task.ToDTO());
    }
}