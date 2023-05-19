namespace Tranga;

public class TaskManager
{
    private readonly Dictionary<Publication, List<Chapter>> _chapterCollection;
    private readonly HashSet<TrangaTask> _allTasks;
    private bool _continueRunning = true;
    
    public TaskManager()
    {
        _chapterCollection = new();
        _allTasks = new ();
        ImportTasks();
        Thread taskChecker = new(TaskCheckerThread);
        taskChecker.Start();
    }

    private void TaskCheckerThread()
    {
        while (_continueRunning)
        {
            foreach (TrangaTask task in _allTasks.Where(trangaTask => trangaTask.ShouldExecute(true)))
            {
                TaskExecutor.Execute(task, this._chapterCollection);
            }
            Thread.Sleep(1000);
        }
    }

    public void Shutdown()
    {
        _continueRunning = false;
        ExportTasks();
    }

    public void ImportTasks()
    {
        throw new NotImplementedException();
    }

    public void ExportTasks()
    {
        throw new NotImplementedException();
    }
}