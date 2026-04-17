using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Tasks.Database;

namespace Services.Tasks.Features.Status;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
public abstract class GetTaskStatusEndpoint
{
    /// <summary>
    /// Get status of a Task
    /// </summary>
    /// <param name="tasksContext"></param>
    /// <param name="taskId">ID of Task</param>
    /// <param name="ct"></param>
    /// <returns>Status of a Task</returns>
    /// <response code="200">Status of a Task</response>
    /// <response code="404">Task with requested ID does not exist</response>
    public static async Task<Results<Ok, NotFound>> Handle(TasksContext tasksContext, [FromRoute] Guid taskId, CancellationToken ct)
    {
        if (await tasksContext.Tasks.SingleOrDefaultAsync(t => t.TaskId == taskId, ct) is not { } task)
            return TypedResults.NotFound();

        throw new NotImplementedException();
    }
}