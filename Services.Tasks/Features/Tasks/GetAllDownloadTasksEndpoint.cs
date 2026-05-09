using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Services.Tasks.Helpers;
using Services.Tasks.Tasks;
using Services.Tasks.WorkerLogic;
using Task = Services.Tasks.Entities.Task;

namespace Services.Tasks.Features.Tasks;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class GetAllDownloadTasksEndpoint
{
    /// <summary>
    /// Get all Download Tasks
    /// </summary>
    /// <param name="includeFinished">Include Download Tasks that have already finished</param>
    /// <returns>List of all Tasks</returns>
    /// <response code="200">List of all Tasks</response>
    public static Ok<Task[]> Handle([FromQuery(Name = "includeFinished")]bool? includeFinished = false)
    {
        IEnumerable<DownloadChapterTask> knownTasks = TasksCollection.GetKnownTasks().OfType<DownloadChapterTask>().Where(t => !t.HasRun || includeFinished == true);
        
        // TODO Pagination

        Task[] result = knownTasks.Select(t => t.ToDto()).ToArray();
        return TypedResults.Ok(result);
    }
}