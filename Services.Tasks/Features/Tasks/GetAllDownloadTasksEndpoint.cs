using Microsoft.AspNetCore.Http.HttpResults;
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
    /// <returns>List of all Tasks</returns>
    /// <response code="200">List of all Tasks</response>
    public static Ok<Task[]> Handle()
    {
        IEnumerable<DownloadChapterTask> knownTasks = TasksCollection.GetKnownTasks().OfType<DownloadChapterTask>();

        Task[] result = knownTasks.Select(t => t.ToDto()).ToArray();
        return TypedResults.Ok(result);
    }
}