using Logging;

namespace Tranga.TrangaTasks;

public class UpdateLibrariesTask : TrangaTask
{
    public UpdateLibrariesTask(TimeSpan reoccurrence) : base(Task.UpdateLibraries, reoccurrence)
    {
    }

    protected override bool ExecuteTask(TaskManager taskManager, Logger? logger, CancellationToken? cancellationToken = null)
    {
        if (cancellationToken?.IsCancellationRequested??false)
            return false;
        foreach(LibraryManager lm in taskManager.settings.libraryManagers)
            lm.UpdateLibrary();
        return true;
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