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
    /// Get Tasks, newest first
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="ct"></param>
    /// <param name="includeFinished">Include Tasks that have already finished</param>
    /// <param name="mangaId">Only Tasks related to this Manga</param>
    /// <param name="taskTypeName">Only Tasks whose concrete type name is in this list</param>
    /// <param name="status">Only Tasks whose <see cref="TaskState"/> is in this list</param>
    /// <param name="skip">Number of Tasks to skip (for pagination)</param>
    /// <param name="limit">Maximum number of Tasks to return</param>
    /// <returns>A page of Tasks, ordered by <see cref="ITask.TaskId"/> descending</returns>
    /// <response code="200">A page of Tasks</response>
    public static async Task<Ok<Task[]>> Handle(
        MangaContext mangaContext,
        CancellationToken ct,
        [FromQuery(Name = "includeFinished")] bool? includeFinished = false,
        [FromQuery(Name = "mangaId")] Guid? mangaId = null,
        [FromQuery(Name = "taskTypeName")] string[]? taskTypeName = null,
        [FromQuery(Name = "status")] TaskState[]? status = null,
        [FromQuery(Name = "skip")] int skip = 0,
        [FromQuery(Name = "limit")] int limit = 25)
    {
        IEnumerable<TaskBase> knownTasks = TasksCollection.GetKnownTasks().Where(t => t is not RunOnceTask r || (!r.HasRun || includeFinished == true));

        if (mangaId is { } mid)
            knownTasks = knownTasks.Where(t => t is IMangaTask m && m.MangaId == mid);
        if (taskTypeName is { Length: > 0 })
            knownTasks = knownTasks.Where(t => taskTypeName.Contains(t.GetType().Name));
        if (status is { Length: > 0 })
            knownTasks = knownTasks.Where(t => status.Contains(t.Status));

        TaskBase[] page = knownTasks.OrderByDescending(t => t.TaskId).Skip(skip).Take(limit).ToArray();

        Task[] result = await page.ToDtosAsync(mangaContext, ct);
        return TypedResults.Ok(result);
    }
}