using Services.Tasks.Database;

namespace Services.Tasks.Helpers;

internal static class WorkerDTOHelper
{
    public static Entities.Worker ToDto(this DbWorker worker) => new()
    {
        WorkerId = worker.WorkerId,
        Status = worker.Status,
        CurrentTaskId = worker.CurrentTaskId,
        CurrentTaskTypeId = worker.CurrentTaskTypeId,
        StartedAt = worker.StartedAt,
        LastHeartbeat = worker.LastHeartbeat
    };
}
