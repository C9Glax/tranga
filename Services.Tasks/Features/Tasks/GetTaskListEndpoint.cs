using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Services.Manga.Database;
using Services.Tasks.Helpers;
using Services.Tasks.TaskTypes;
using Services.Tasks.WorkerLogic;
using Task = Services.Tasks.Entities.Task;

namespace Services.Tasks.Features.Tasks;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class GetTaskListEndpoint
{
    /// <summary>
    /// Get all Tasks
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="includeFinished">Include Tasks that have already finished</param>
    /// <param name="ct"></param>
    /// <returns>List of all Tasks</returns>
    /// <response code="200">List of all Tasks</response>
    public static async Task<Ok<Task[]>> Handle(MangaContext mangaContext, CancellationToken ct, [FromQuery(Name = "includeFinished")]bool? includeFinished = false)
    {
        IEnumerable<TaskBase> knownTasks = TasksCollection.GetKnownTasks().Where(t => t is not RunOnceTask r || (!r.HasRun || includeFinished == true));

        // TODO Pagination

        Task[] result = await knownTasks.ToDtosAsync(mangaContext, ct);
        return TypedResults.Ok(result);
    }
}