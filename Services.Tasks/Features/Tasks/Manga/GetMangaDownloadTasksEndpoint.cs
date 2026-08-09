using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Services.Manga.Database;
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
    /// <param name="mangaContext"></param>
    /// <param name="mangaId">Id of the Manga</param>
    /// <param name="ct"></param>
    /// <param name="includeFinished">Include Download Tasks that have already finished</param>
    /// <returns>List of all Tasks</returns>
    /// <response code="200">List of all Tasks</response>
    public static async Task<Ok<Task[]>> Handle(MangaContext mangaContext, [FromRoute]Guid mangaId, CancellationToken ct, [FromQuery(Name = "includeFinished")]bool? includeFinished = false)
    {
        IEnumerable<DownloadChapterTask> knownTasks =
            TasksCollection.GetKnownTasks().FilterManga(mangaId).OfType<DownloadChapterTask>().Where(t => !t.HasRun || includeFinished == true);

        // TODO Pagination

        Task[] result = await knownTasks.ToDtosAsync(mangaContext, ct);
        return TypedResults.Ok(result);
    }
}