using Logging;

namespace Tranga.TrangaTasks;

public class UpdateKomgaLibraryTask : TrangaTask
{
    public UpdateKomgaLibraryTask(Task task, TimeSpan reoccurrence) : base(task, null, null, reoccurrence)
    {
    }

    protected override void ExecuteTask(TaskManager taskManager, Logger? logger)
    {
        taskManager.komga?.UpdateLibrary();
    }
}