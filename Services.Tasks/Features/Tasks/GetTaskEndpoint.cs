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
internal abstract class GetTaskEndpoint
{
    /// <summary>
    /// Get Task
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="taskId">ID of Task</param>
    /// <param name="ct"></param>
    /// <returns>Task</returns>
    /// <response code="200">Task</response>
    /// <response code="404">Task with requested ID does not exist</response>
    public static async Task<Results<Ok<Task>, NotFound>> Handle(MangaContext mangaContext, [FromRoute] Guid taskId, CancellationToken ct)
    {
        if (TasksCollection.GetTask(taskId) is not { } task)
            return TypedResults.NotFound();

        Task dto = (await new ITask[] { task }.ToDtosAsync(mangaContext, ct)).Single();
        return TypedResults.Ok(dto);
    }
}