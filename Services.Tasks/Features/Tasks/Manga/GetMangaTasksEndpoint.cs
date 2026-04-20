using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Services.Tasks.Helpers;
using Services.Tasks.TaskTypes;
using Services.Tasks.WorkerLogic;

namespace Services.Tasks.Features.Tasks.Manga;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class GetMangaTasksEndpoint
{
    /// <summary>
    /// Get Tasks related to a Manga
    /// </summary>
    /// <returns>List of all Tasks</returns>
    /// <response code="200">List of all Tasks</response>
    public static Ok<Entities.MangaTask[]> Handle([FromRoute]Guid mangaId)
    {
        IEnumerable<IMangaTask> knownTasks = TasksCollection.GetKnownTasks().RelatedToManga(mangaId);

        Entities.MangaTask[] result = knownTasks.Select(t => t.ToDTO()).ToArray();
        return TypedResults.Ok(result);
    }
}