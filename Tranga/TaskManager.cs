namespace Tranga;

public class TaskManager
{
    private Dictionary<Publication, Chapter[]> _chapterCollection;
    private readonly HashSet<TrangaTask> _allTasks;
    private bool _continueRunning = true;
    
    public TaskManager()
    {
        _chapterCollection = new();
        _allTasks = new ();
        Thread taskChecker = new(TaskCheckerThread);
        taskChecker.Start();
    }

    public void AddTask(Connector connector, TrangaTask.AvailableTasks task, TimeSpan reoccurrence, Publication? publication = null, string language = "en")
    {
        this._allTasks.Add(new TrangaTask(connector, task, reoccurrence, publication, language));
    }

    private void TaskCheckerThread()
    {
        while (_continueRunning)
        {
            foreach (TrangaTask task in _allTasks.Where(trangaTask => (DateTime.Now - trangaTask.lastExecuted) > trangaTask.reoccurrence))
            {
                task.Execute(ref _chapterCollection);
            }
            Thread.Sleep(1000);
        }
    }

    public bool PublicationAlreadyAdded(Publication publication)
    {
        throw new NotImplementedException();
        //TODO fuzzy check publications
    }

    public Publication[] GetAddedPublications()
    {
        return this._chapterCollection.Keys.ToArray();
    }

    public TrangaTask[] GetTasks()
    {
        return _allTasks.ToArray();
    }

    public void Shutdown()
    {
        _continueRunning = false;
    }
}