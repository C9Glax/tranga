using Microsoft.AspNetCore.Http.HttpResults;
using Services.Manga.Database;
using Services.Tasks.Helpers;
using Services.Tasks.Tasks;
using Services.Tasks.TaskTypes;
using Services.Tasks.WorkerLogic;
using Task = Services.Tasks.Entities.Task;

namespace Services.Tasks.Features.Tasks;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class PutBackfillComicInfoTaskEndpoint
{
    /// <summary>
    /// Create a <see cref="BackfillComicInfoTask"/> that adds a <c>ComicInfo.xml</c> entry to every
    /// already-downloaded Chapter archive that is missing one.
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="ct"></param>
    /// <returns>Created Task</returns>
    /// <response code="200">Created Task</response>
    /// <response code="500">Failed to add Task</response>
    public static async Task<Results<Ok<Task>, InternalServerError>> Handle(MangaContext mangaContext, CancellationToken ct)
    {
        BackfillComicInfoTask task = new();
        if (!TasksCollection.RunOnceTasks.TryAdd(task.TaskId, task))
            return TypedResults.InternalServerError();

        Task dto = (await new ITask[] { task }.ToDtosAsync(mangaContext, ct)).Single();
        return TypedResults.Ok(dto);
    }
}
