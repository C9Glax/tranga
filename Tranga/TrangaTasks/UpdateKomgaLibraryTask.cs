using Logging;

namespace Tranga.TrangaTasks;

public class UpdateKomgaLibraryTask : TrangaTask
{
    public UpdateKomgaLibraryTask(Task task, TimeSpan reoccurrence) : base(task, null, null, reoccurrence)
    {
    }

    protected override void ExecuteTask(TaskManager taskManager, Logger? logger)
    {
        if (taskManager.komga is null)
            return;
        Komga komga = taskManager.komga;

        Komga.KomgaLibrary[] allLibraries = komga.GetLibraries();
        foreach (Komga.KomgaLibrary lib in allLibraries)
            komga.UpdateLibrary(lib.id);
    }
}