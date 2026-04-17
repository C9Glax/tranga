using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Tasks.Database;
using Services.Tasks.Helpers;
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
    /// <param name="tasksContext"></param>
    /// <param name="taskId">ID of Task</param>
    /// <param name="ct"></param>
    /// <returns>Task</returns>
    /// <response code="200">Task</response>
    /// <response code="404">Task with requested ID does not exist</response>
    public static async Task<Results<Ok<Task>, NotFound>> Handle(TasksContext tasksContext, [FromRoute] Guid taskId, CancellationToken ct)
    {
        if (await tasksContext.Tasks.SingleOrDefaultAsync(t => t.TaskId == taskId, ct) is not { } task)
            return TypedResults.NotFound();

        return TypedResults.Ok(task.ToDTO());
    }
}