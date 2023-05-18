namespace Tranga;

public class TaskManager
{
    private readonly Dictionary<Publication, TrangaTask> _allTasks;
    private bool _continueRunning = true;
    
    public TaskManager()
    {
        _allTasks = new Dictionary<Publication, TrangaTask>();
        Thread taskChecker = new(TaskCheckerThread);
        taskChecker.Start();
    }

    private void TaskCheckerThread()
    {
        while (_continueRunning)
        {
            foreach (TrangaTask task in _allTasks.Values.Where(tt => (DateTime.Now - tt.lastExecuted) > tt.reoccurrence))
            {
                if (!task.lastExecutedSuccessfully)
                {
                    task.Abort();
                    //Add logging that task has failed
                }
                task.Execute();
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
        return _allTasks.Keys.ToArray();
    }

    public TrangaTask[] GetTasks()
    {
        return _allTasks.Values.ToArray();
    }

    public void Shutdown()
    {
        _continueRunning = false;
    }
}