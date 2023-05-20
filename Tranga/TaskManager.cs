using Logging;
using Newtonsoft.Json;
using Tranga.Connectors;

namespace Tranga;

/// <summary>
/// Manages all TrangaTasks.
/// Provides a Threaded environment to execute Tasks, and still manage the Task-Collection
/// </summary>
public class TaskManager
{
    private readonly Dictionary<Publication, List<Chapter>> _chapterCollection = new();
    private readonly HashSet<TrangaTask> _allTasks;
    private bool _continueRunning = true;
    private readonly Connector[] _connectors;
    private Dictionary<Connector, List<TrangaTask>> tasksToExecute = new();
    private string downloadLocation { get; }
    private Logger? logger { get; }
    
    public Komga? komga { get; }

    /// <param name="folderPath">Local path to save data (Manga) to</param>
    /// <param name="komgaBaseUrl">The Url of the Komga-instance that you want to update</param>
    /// <param name="komgaUsername">The Komga username</param>
    /// <param name="komgaPassword">The Komga password</param>
    /// <param name="logger"></param>
    public TaskManager(string folderPath, string? komgaBaseUrl = null, string? komgaUsername = null, string? komgaPassword = null, Logger? logger = null)
    {
        this.logger = logger;
        this.downloadLocation = folderPath;

        if (komgaBaseUrl != null && komgaUsername != null && komgaPassword != null)
            this.komga = new Komga(komgaBaseUrl, komgaUsername, komgaPassword, logger);
        this._connectors = new Connector[]{ new MangaDex(folderPath, logger) };
        foreach(Connector cConnector in this._connectors)
            tasksToExecute.Add(cConnector, new List<TrangaTask>());
        _allTasks = new HashSet<TrangaTask>();
        
        Thread taskChecker = new(TaskCheckerThread);
        taskChecker.Start();
    }

    public TaskManager(SettingsData settings, Logger? logger = null)
    {
        this.logger = logger;
        this._connectors = new Connector[]{ new MangaDex(settings.downloadLocation, logger) };
        foreach(Connector cConnector in this._connectors)
            tasksToExecute.Add(cConnector, new List<TrangaTask>());
        this.downloadLocation = settings.downloadLocation;
        this.komga = settings.komga;
        _allTasks = settings.allTasks;
        Thread taskChecker = new(TaskCheckerThread);
        taskChecker.Start();
    }

    /// <summary>
    /// Runs continuously until shutdown.
    /// Checks if tasks have to be executed (time elapsed)
    /// </summary>
    private void TaskCheckerThread()
    {
        logger?.WriteLine(this.GetType().ToString(), "Starting TaskCheckerThread.");
        while (_continueRunning)
        {
            //Check if previous tasks have finished and execute new tasks
            foreach (KeyValuePair<Connector, List<TrangaTask>> connectorTaskQueue in tasksToExecute)
            {
                connectorTaskQueue.Value.RemoveAll(task => task.state == TrangaTask.ExecutionState.Waiting);
                if (connectorTaskQueue.Value.Count > 0 && connectorTaskQueue.Value.All(task =>
                        task.state is TrangaTask.ExecutionState.Running or TrangaTask.ExecutionState.Enqueued))
                {
                    ExecuteTaskNow(connectorTaskQueue.Value.First());
                    ExportData(Directory.GetCurrentDirectory());
                }
            }
            
            //Check if task should be executed
            //Depending on type execute immediately or enqueue
            foreach (TrangaTask task in _allTasks.Where(aTask => aTask.ShouldExecute()))
            {
                task.state = TrangaTask.ExecutionState.Enqueued;
                if(task.connectorName is null)
                    ExecuteTaskNow(task);
                else
                {
                    logger?.WriteLine(this.GetType().ToString(), $"Task due: {task}");
                    tasksToExecute[GetConnector(task.connectorName!)].Add(task);
                }
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
        
        logger?.WriteLine(this.GetType().ToString(), $"Forcing Execution: {task}");
        Task t = new Task(() =>
        {
            TaskExecutor.Execute(this, task, this._chapterCollection, logger);
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
    public TrangaTask AddTask(TrangaTask.Task task, string? connectorName, Publication? publication, TimeSpan reoccurrence,
        string language = "")
    {
        logger?.WriteLine(this.GetType().ToString(), $"Adding new Task {task} {connectorName} {publication?.sortName}");
        if (task != TrangaTask.Task.UpdateKomgaLibrary && connectorName is null)
            throw new ArgumentException($"connectorName can not be null for task {task}");

        TrangaTask newTask;
        if (task == TrangaTask.Task.UpdateKomgaLibrary)
        {
            newTask = new TrangaTask(task, null, null, reoccurrence, language);
            
            //Check if same task already exists
            // ReSharper disable once SimplifyLinqExpressionUseAll readabilty
            if (!_allTasks.Any(trangaTask => trangaTask.task == task))
            {
                _allTasks.Add(newTask);
            }
        }
        else
        {
            //Get appropriate Connector from available Connectors for TrangaTask
            Connector? connector = _connectors.FirstOrDefault(c => c.name == connectorName);
            if (connector is null)
                throw new ArgumentException($"Connector {connectorName} is not a known connector.");
        
            newTask = new TrangaTask(task, connector.name, publication, reoccurrence, language);
            
            //Check if same task already exists
            if (!_allTasks.Any(trangaTask => trangaTask.task == task && trangaTask.connectorName == connector.name &&
                                             trangaTask.publication?.downloadUrl == publication?.downloadUrl))
            {
                if(task != TrangaTask.Task.UpdatePublications)
                    _chapterCollection.Add((Publication)publication!, new List<Chapter>());
                _allTasks.Add(newTask);
            }
        }
        logger?.WriteLine(this.GetType().ToString(), $"Added new Task {newTask.ToString()}");
        ExportData(Directory.GetCurrentDirectory());
        
        return newTask;
    }

    /// <summary>
    /// Removes Task from task-collection
    /// </summary>
    /// <param name="task">TrangaTask.Task type</param>
    /// <param name="connectorName">Name of Connector that was used</param>
    /// <param name="publication">Publication that was used</param>
    public void RemoveTask(TrangaTask.Task task, string? connectorName, Publication? publication)
    {
        logger?.WriteLine(this.GetType().ToString(), $"Removing Task {task} {publication?.sortName}");
        if (task == TrangaTask.Task.UpdateKomgaLibrary)
        {
            _allTasks.RemoveWhere(uTask => uTask.task == TrangaTask.Task.UpdateKomgaLibrary);
            logger?.WriteLine(this.GetType().ToString(), $"Removed Task {task}");
        }
        else if (connectorName is null)
            throw new ArgumentException($"connectorName can not be null for Task {task}");
        else
        {
            if(_allTasks.RemoveWhere(trangaTask =>
                trangaTask.task == task && trangaTask.connectorName == connectorName &&
                trangaTask.publication?.downloadUrl == publication?.downloadUrl) > 0)
                logger?.WriteLine(this.GetType().ToString(), $"Removed Task {task} {publication?.sortName} {publication?.downloadUrl}.");
            else
                logger?.WriteLine(this.GetType().ToString(), $"No Task {task} {publication?.sortName} {publication?.downloadUrl} could be found.");
        }
        ExportData(Directory.GetCurrentDirectory());
    }
    
    /// <returns>All available Connectors</returns>
    public Dictionary<string, Connector> GetAvailableConnectors()
    {
        return this._connectors.ToDictionary(connector => connector.name, connector => connector);
    }
    
    /// <returns>All TrangaTasks in task-collection</returns>
    public TrangaTask[] GetAllTasks()
    {
        TrangaTask[] ret = new TrangaTask[_allTasks.Count];
        _allTasks.CopyTo(ret);
        return ret;
    }
    
    /// <returns>All added Publications</returns>
    public Publication[] GetAllPublications()
    {
        return this._chapterCollection.Keys.ToArray();
    }

    /// <summary>
    /// Return Connector with given Name
    /// </summary>
    /// <param name="connectorName">Connector-name (exact)</param>
    /// <exception cref="Exception">If Connector is not available</exception>
    public Connector GetConnector(string connectorName)
    {
        if(connectorName is null)
            throw new Exception($"connectorName can not be null");
        Connector? ret = this._connectors.FirstOrDefault(connector => connector.name == connectorName);
        if (ret is null)
            throw new Exception($"Connector {connectorName} is not an available Connector.");
        return (Connector)ret!;
    }
    
    /// <summary>
    /// Shuts down the taskManager.
    /// </summary>
    /// <param name="force">If force is true, tasks are aborted.</param>
    public void Shutdown(bool force = false)
    {
        logger?.WriteLine(this.GetType().ToString(), $"Shutting down (forced={force})");
        _continueRunning = false;
        ExportData(Directory.GetCurrentDirectory());
        
        if(force)
            Environment.Exit(_allTasks.Count(task => task.state is TrangaTask.ExecutionState.Enqueued or TrangaTask.ExecutionState.Running));
        
        //Wait for tasks to finish
        while(_allTasks.Any(task => task.state is TrangaTask.ExecutionState.Running or TrangaTask.ExecutionState.Enqueued))
            Thread.Sleep(10);
        logger?.WriteLine(this.GetType().ToString(), "Tasks finished. Bye!");
        Environment.Exit(0);
    }

    /// <summary>
    /// Loads stored data (settings, tasks) from file
    /// </summary>
    /// <param name="importFolderPath">working directory, filename has to be data.json</param>
    public static SettingsData LoadData(string importFolderPath)
    {
        string importPath = Path.Join(importFolderPath, "data.json");
        if (!File.Exists(importPath))
            return new SettingsData("", null, new HashSet<TrangaTask>());

        string toRead = File.ReadAllText(importPath);
        SettingsData data = JsonConvert.DeserializeObject<SettingsData>(toRead)!;

        return data;
    }

    /// <summary>
    /// Exports data (settings, tasks) to file
    /// </summary>
    /// <param name="exportFolderPath">Folder path, filename will be data.json</param>
    private void ExportData(string exportFolderPath)
    {
        logger?.WriteLine(this.GetType().ToString(), $"Exporting data to data.json");
        SettingsData data = new SettingsData(this.downloadLocation, this.komga, this._allTasks);

        string exportPath = Path.Join(exportFolderPath, "data.json");
        string serializedData = JsonConvert.SerializeObject(data);
        File.Delete(exportPath);
        File.WriteAllText(exportPath, serializedData);
    }

    public class SettingsData
    {
        public string downloadLocation { get; set; }
        public Komga? komga { get; set; }
        public HashSet<TrangaTask> allTasks { get; }

        public SettingsData(string downloadLocation, Komga? komga, HashSet<TrangaTask> allTasks)
        {
            this.downloadLocation = downloadLocation;
            this.komga = komga;
            this.allTasks = allTasks;
        }
    }
}