using System.Net;
using Logging;

namespace Tranga.TrangaTasks;

/// <summary>
/// LEGACY DEPRECATED
/// </summary>
public class UpdateLibrariesTask : TrangaTask
{
    public UpdateLibrariesTask(TimeSpan reoccurrence) : base(Task.UpdateLibraries, reoccurrence)
    {
    }

    protected override HttpStatusCode ExecuteTask(TaskManager taskManager, Logger? logger, CancellationToken? cancellationToken = null)
    {
        return HttpStatusCode.BadRequest;
    }

    public override TrangaTask Clone()
    {
        return new UpdateLibrariesTask(this.reoccurrence);
    }

    protected override double GetProgress()
    {
        return 1;
    }
}