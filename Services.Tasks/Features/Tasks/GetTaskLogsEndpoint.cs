using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Services.Tasks.WorkerLogic;
using TaskLogEntry = Services.Tasks.Entities.TaskLogEntry;

namespace Services.Tasks.Features.Tasks;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class GetTaskLogsEndpoint
{
    /// <summary>
    /// Get Log entries for a Task, oldest first
    /// </summary>
    /// <param name="taskId">ID of Task</param>
    /// <param name="skip">Number of Log entries to skip (for pagination)</param>
    /// <param name="limit">Maximum number of Log entries to return</param>
    /// <returns>A page of Log entries, ordered chronologically</returns>
    /// <response code="200">A page of Log entries</response>
    /// <response code="404">Task with requested ID does not exist</response>
    public static Results<Ok<TaskLogEntry[]>, NotFound> Handle(
        [FromRoute] Guid taskId,
        [FromQuery(Name = "skip")] int skip = 0,
        [FromQuery(Name = "limit")] int limit = 200)
    {
        if (TasksCollection.GetTask(taskId) is not { } task)
            return TypedResults.NotFound();

        TaskLogEntry[] result = task.LogEntries
            .Skip(skip)
            .Take(limit)
            .Select(e => new TaskLogEntry { Timestamp = e.Timestamp, Level = e.Level.ToString(), Message = e.Message })
            .ToArray();
        return TypedResults.Ok(result);
    }
}
