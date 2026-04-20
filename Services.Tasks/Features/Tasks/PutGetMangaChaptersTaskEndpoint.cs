using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Tasks.Helpers;
using Services.Tasks.Tasks;
using Services.Tasks.WorkerLogic;

namespace Services.Tasks.Features.Tasks;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class PutGetMangaChaptersTaskEndpoint
{
    /// <summary>
    /// Create a <see cref="GetMangaChaptersTask"/> for Manga with the requested ID.
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="mangaId">ID of <see cref="DbManga"/></param>
    /// <param name="ct"></param>
    /// <returns>Created Task</returns>
    /// <response code="200">Created Task</response>
    /// <response code="404">Manga with requested ID does not exist</response>
    /// <response code="500">Failed to add Task</response>
    public static async Task<Results<Ok<Entities.MangaTask>, NotFound, InternalServerError>> Handle(MangaContext mangaContext, [FromRoute] Guid mangaId, CancellationToken ct)
    {
        if (!await mangaContext.Mangas.AnyAsync(m => m.MangaId == mangaId, ct))
            return TypedResults.NotFound();

        GetMangaChaptersTask task = new (mangaId);
        if (!TasksCollection.RunOnceTasks.TryAdd(task.TaskId, task))
            return TypedResults.InternalServerError();

        return TypedResults.Ok(task.ToDTO());
    }
}