using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Services.Tasks.Database;
using Services.Tasks.Helpers;

namespace Services.Tasks.Features.Tasks;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
public abstract class GetTaskListEndpoint
{
    /// <summary>
    /// Get all Tasks
    /// </summary>
    /// <param name="tasksContext"></param>
    /// <param name="ct"></param>
    /// <returns>List of all Tasks</returns>
    /// <response code="200">List of all Tasks</response>
    /// <response code="500">List of all Tasks</response>
    public static async Task<Results<Ok<Entities.Task[]>, InternalServerError>> Handle(TasksContext tasksContext, CancellationToken ct)
    {
        if (await tasksContext.Tasks.ToListAsync(ct) is not { } tasks)
            return TypedResults.InternalServerError();

        Entities.Task[] result = tasks.Select(t => t.ToDTO()).ToArray();
        return TypedResults.Ok(result);
    }
}