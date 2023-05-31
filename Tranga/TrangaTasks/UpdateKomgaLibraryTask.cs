namespace Tranga.TrangaTasks;

public class UpdateKomgaLibraryTask : TrangaTask
{
    public UpdateKomgaLibraryTask(Task task, TimeSpan reoccurrence) : base(task, null, null, reoccurrence)
    {
    }

    public override void Execute(TaskManager taskManager)
    {
        this.state = ExecutionState.Running;
        if (taskManager.komga is null)
            return;
        Komga komga = taskManager.komga;

        Komga.KomgaLibrary[] allLibraries = komga.GetLibraries();
        foreach (Komga.KomgaLibrary lib in allLibraries)
            komga.UpdateLibrary(lib.id);
        
        this.lastExecuted = DateTime.Now;
        this.state = ExecutionState.Waiting;
    }
}