using Logging;

namespace Tranga.TrangaTasks;

public class UpdateLibrariesTask : TrangaTask
{
    public UpdateLibrariesTask(Task task, TimeSpan reoccurrence) : base(task, reoccurrence)
    {
    }

    protected override void ExecuteTask(TaskManager taskManager, Logger? logger)
    {
        foreach(LibraryManager lm in taskManager.settings.libraryManagers)
            lm.UpdateLibrary();
        this.progress = 1f;
    }
}