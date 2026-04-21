using Microsoft.AspNetCore.Http.HttpResults;
using Services.Tasks.Helpers;
using Services.Tasks.Tasks;
using Services.Tasks.WorkerLogic;

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
    public static Ok<Entities.ChapterTask[]> Handle()
    {
        IEnumerable<DownloadChapterTask> knownTasks = TasksCollection.GetKnownTasks().OfType<DownloadChapterTask>();

        Entities.ChapterTask[] result = knownTasks.Select(t => t.ToDTO()).ToArray();
        return TypedResults.Ok(result);
    }
}