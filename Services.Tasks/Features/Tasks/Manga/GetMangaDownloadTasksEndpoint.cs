using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Services.Tasks.Helpers;
using Services.Tasks.Tasks;
using Services.Tasks.WorkerLogic;
using Task = Services.Tasks.Entities.Task;

namespace Services.Tasks.Features.Tasks.Manga;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class GetMangaDownloadTasksEndpoint
{
    /// <summary>
    /// Get the Download Tasks related to a Manga
    /// </summary>
    /// <param name="mangaId">Id of the Manga</param>
    /// <param name="includeFinished">Include Download Tasks that have already finished</param>
    /// <returns>List of all Tasks</returns>
    /// <response code="200">List of all Tasks</response>
    public static Ok<Task[]> Handle([FromRoute]Guid mangaId, [FromQuery(Name = "includeFinished")]bool? includeFinished = false)
    {
        IEnumerable<DownloadChapterTask> knownTasks =
            TasksCollection.GetKnownTasks().FilterManga(mangaId).OfType<DownloadChapterTask>().Where(t => !t.HasRun || includeFinished == true);
        
        // TODO Pagination

        Task[] result = knownTasks.Select(t => t.ToDto()).ToArray();
        return TypedResults.Ok(result);
    }
}