using Logging;

namespace Tranga.TrangaTasks;

public class UpdateLibrariesTask : TrangaTask
{
    public UpdateLibrariesTask(Task task, TimeSpan reoccurrence) : base(task, reoccurrence)
    {
    }

    protected override void ExecuteTask(TaskManager taskManager, Logger? logger, CancellationToken? cancellationToken = null)
    {
        if (cancellationToken?.IsCancellationRequested??false)
            return;
        foreach(LibraryManager lm in taskManager.settings.libraryManagers)
            lm.UpdateLibrary();
        IncrementProgress(1);
    }
}