using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
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
    /// <param name="includeFinished">Include Tasks that have already finished</param>
    /// <returns>List of all Tasks</returns>
    /// <response code="200">List of all Tasks</response>
    public static Ok<Task[]> Handle([FromRoute]Guid mangaId, [FromQuery(Name = "includeFinished")]bool? includeFinished = false)
    {
        IEnumerable<IMangaTask> knownTasks = TasksCollection.GetKnownTasks().FilterManga(mangaId).Where(t => t is not RunOnceTask r || (!r.HasRun || includeFinished == true));
        
        // TODO Pagination

        Task[] result = knownTasks.Select(t => t.ToDto()).ToArray();
        return TypedResults.Ok(result);
    }
}