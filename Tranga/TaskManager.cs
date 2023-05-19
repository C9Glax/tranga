using Newtonsoft.Json;
using Tranga.Connectors;

namespace Tranga;

/// <summary>
/// Manages all TrangaTasks.
/// Provides a Threaded environment to execute Tasks, and still manage the Task-Collection
/// </summary>
public class TaskManager
{
    private readonly Dictionary<Publication, List<Chapter>> _chapterCollection;
    private readonly HashSet<TrangaTask> _allTasks;
    private bool _continueRunning = true;
    private readonly Connector[] connectors;
    private readonly string folderPath;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="folderPath">Local path to save data (Manga) to</param>
    public TaskManager(string folderPath)
    {
        this.folderPath = folderPath;
        this.connectors = new Connector[]{ new MangaDex(folderPath) };
        _chapterCollection = new();
        _allTasks = ImportTasks(Directory.GetCurrentDirectory());
        Thread taskChecker = new(TaskCheckerThread);
        taskChecker.Start();
    }

    private void TaskCheckerThread()
    {
        while (_continueRunning)
        {
            foreach (TrangaTask task in _allTasks)
            {
                if(task.ShouldExecute()) 
                    TaskExecutor.Execute(this.connectors, task, this._chapterCollection);
            }
            Thread.Sleep(1000);
        }
    }

    /// <summary>
    /// Forces the execution of a given task
    /// </summary>
    /// <param name="task">Task to execute</param>
    public void ExecuteTaskNow(TrangaTask task)
    {
        if (!this._allTasks.Contains(task))
            return;
        
        Task t = new Task(() =>
        {
            TaskExecutor.Execute(this.connectors, task, this._chapterCollection);
        });
        t.Start();
    }

    /// <summary>
    /// Creates and adds a new Task to the task-Collection
    /// </summary>
    /// <param name="task">TrangaTask.Task to later execute</param>
    /// <param name="connectorName">Name of the connector to use</param>
    /// <param name="publication">Publication to execute Task on, can be null in case of unrelated Task</param>
    /// <param name="reoccurrence">Time-Interval between Executions</param>
    /// <param name="language">language, should Task require parameter. Can be empty</param>
    /// <exception cref="ArgumentException">Is thrown when connectorName is not a available Connector</exception>
    public void AddTask(TrangaTask.Task task, string connectorName, Publication? publication, TimeSpan reoccurrence,
        string language = "")
    {
        Connector? connector = connectors.FirstOrDefault(c => c.name == connectorName);
        if (connector is null)
            throw new ArgumentException($"Connector {connectorName} is not a known connector.");
        
        if (!_allTasks.Any(trangaTask => trangaTask.task != task && trangaTask.connectorName != connector.name &&
                                         trangaTask.publication?.downloadUrl != publication?.downloadUrl))
        {
            if(task != TrangaTask.Task.UpdatePublications)
                _chapterCollection.Add((Publication)publication!, new List<Chapter>());
            _allTasks.Add(new TrangaTask(connector.name, task, publication, reoccurrence, language));
            ExportTasks(Directory.GetCurrentDirectory());
        }
    }

    /// <summary>
    /// Removes Task from task-collection
    /// </summary>
    /// <param name="task">TrangaTask.Task type</param>
    /// <param name="connectorName">Name of Connector that was used</param>
    /// <param name="publication">Publication that was used</param>
    public void RemoveTask(TrangaTask.Task task, string connectorName, Publication? publication)
    {
        _allTasks.RemoveWhere(trangaTask =>
            trangaTask.task == task && trangaTask.connectorName == connectorName &&
            trangaTask.publication?.downloadUrl == publication?.downloadUrl);
        ExportTasks(Directory.GetCurrentDirectory());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>All available Connectors</returns>
    public Dictionary<string, Connector> GetAvailableConnectors()
    {
        return this.connectors.ToDictionary(connector => connector.name, connector => connector);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>All TrangaTasks in task-collection</returns>
    public TrangaTask[] GetAllTasks()
    {
        TrangaTask[] ret = new TrangaTask[_allTasks.Count];
        _allTasks.CopyTo(ret);
        return ret;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>All added Publications</returns>
    public Publication[] GetAllPublications()
    {
        return this._chapterCollection.Keys.ToArray();
    }
    
    /// <summary>
    /// Shuts down the taskManager.
    /// </summary>
    /// <param name="force">If force is true, tasks are aborted.</param>
    public void Shutdown(bool force = false)
    {
        _continueRunning = false;
        ExportTasks(Directory.GetCurrentDirectory());
        
        if(force)
            Environment.Exit(_allTasks.Count(task => task.isBeingExecuted));
        
        //Wait for tasks to finish
        while(_allTasks.Any(task => task.isBeingExecuted))
            Thread.Sleep(10);
        
    }

    private HashSet<TrangaTask> ImportTasks(string importFolderPath)
    {
        string filePath = Path.Join(importFolderPath, "tasks.json");
        if (!File.Exists(filePath))
            return new HashSet<TrangaTask>();

        string toRead = File.ReadAllText(filePath);

        TrangaTask[] importTasks = JsonConvert.DeserializeObject<TrangaTask[]>(toRead)!;
        
        foreach(TrangaTask task in importTasks.Where(task => task.publication is not null))
            this._chapterCollection.Add((Publication)task.publication!, new List<Chapter>());
        
        return importTasks.ToHashSet();
    }

    private void ExportTasks(string exportFolderPath)
    {
        string filePath = Path.Join(exportFolderPath, "tasks.json");
        string toWrite = JsonConvert.SerializeObject(_allTasks.ToArray());
        File.WriteAllText(filePath,toWrite);
    }
}