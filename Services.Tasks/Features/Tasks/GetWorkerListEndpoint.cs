using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Services.Tasks.Database;
using Services.Tasks.Helpers;

namespace Services.Tasks.Features.Tasks;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class GetWorkerListEndpoint
{
    /// <summary>
    /// Get all currently active Workers
    /// </summary>
    /// <returns>List of all Workers</returns>
    /// <response code="200">List of all Workers</response>
    public static async Task<Ok<Entities.Worker[]>> Handle(TasksContext tasksContext, CancellationToken ct)
    {
        List<DbWorker> workers = await tasksContext.Workers.ToListAsync(ct);
        Entities.Worker[] result = workers.Select(w => w.ToDto()).ToArray();
        return TypedResults.Ok(result);
    }
}
