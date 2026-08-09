using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Services.Manga.Database;
using Services.Tasks.Helpers;
using Services.Tasks.TaskTypes;
using Services.Tasks.WorkerLogic;
using Task = Services.Tasks.Entities.Task;

namespace Services.Tasks.Features.Tasks.Manga;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class GetMangaTasksEndpoint
{
    /// <summary>
    /// Get Tasks related to a Manga
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="mangaId"></param>
    /// <param name="ct"></param>
    /// <param name="includeFinished">Include Tasks that have already finished</param>
    /// <returns>List of all Tasks</returns>
    /// <response code="200">List of all Tasks</response>
    public static async Task<Ok<Task[]>> Handle(MangaContext mangaContext, [FromRoute]Guid mangaId, CancellationToken ct, [FromQuery(Name = "includeFinished")]bool? includeFinished = false)
    {
        IEnumerable<IMangaTask> knownTasks = TasksCollection.GetKnownTasks().FilterManga(mangaId).Where(t => t is not RunOnceTask r || (!r.HasRun || includeFinished == true));

        // TODO Pagination

        Task[] result = await knownTasks.ToDtosAsync(mangaContext, ct);
        return TypedResults.Ok(result);
    }
}