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
    private readonly Dictionary<Connector, List<TrangaTask>> _taskQueue = new();
    public SettingsData settings { get; }
    private Logger? logger { get; }
    
    public Komga? komga { get; }

    /// <param name="downloadFolderPath">Local path to save data (Manga) to</param>
    /// <param name="settingsFilePath">Path to the settings file (data.json)</param>
    /// <param name="komgaBaseUrl">The Url of the Komga-instance that you want to update</param>
    /// <param name="komgaUsername">The Komga username</param>
    /// <param name="komgaPassword">The Komga password</param>
    /// <param name="logger"></param>
    public TaskManager(string downloadFolderPath, string? settingsFilePath = null, string? komgaBaseUrl = null, string? komgaUsername = null, string? komgaPassword = null, Logger? logger = null)
    {
        this.logger = logger;
        _allTasks = new HashSet<TrangaTask>();
        if (komgaBaseUrl != null && komgaUsername != null && komgaPassword != null)
            this.komga = new Komga(komgaBaseUrl, komgaUsername, komgaPassword, logger);
        
        this.settings = new SettingsData(downloadFolderPath, settingsFilePath, this.komga, this._allTasks);
        ExportData();
        
        this._connectors = new Connector[]{ new MangaDex(downloadFolderPath, logger) };
        foreach(Connector cConnector in this._connectors)
            _taskQueue.Add(cConnector, new List<TrangaTask>());
        
        Thread taskChecker = new(TaskCheckerThread);
        taskChecker.Start();
    }

    public TaskManager(SettingsData settings, Logger? logger = null)
    {
        this.logger = logger;
        this._connectors = new Connector[]{ new MangaDex(settings.downloadLocation, logger) };
        this.settings = settings;
        ExportData();
        
        foreach(Connector cConnector in this._connectors)
            _taskQueue.Add(cConnector, new List<TrangaTask>());
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
            foreach (KeyValuePair<Connector, List<TrangaTask>> connectorTaskQueue in _taskQueue)
            {
                if(connectorTaskQueue.Value.RemoveAll(task => task.state == TrangaTask.ExecutionState.Waiting) > 0)
                    ExportData();
                
                if (connectorTaskQueue.Value.Count > 0 && connectorTaskQueue.Value.All(task => task.state is TrangaTask.ExecutionState.Enqueued))
                    ExecuteTaskNow(connectorTaskQueue.Value.First());
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
                    _taskQueue[GetConnector(task.connectorName!)].Add(task);
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
            if(connectorName is null)
                throw new ArgumentException($"connectorName can not be null for task {task}");
            
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
        ExportData();
        
        return newTask;
    }

    /// <summary>
    /// Removes Task from task-collection
    /// </summary>
    /// <param name="task">TrangaTask.Task type</param>
    /// <param name="connectorName">Name of Connector that was used</param>
    /// <param name="publication">Publication that was used</param>
    public void DeleteTask(TrangaTask.Task task, string? connectorName, Publication? publication)
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
        ExportData();
    }

    /// <summary>
    /// Removes a Task from the queue
    /// </summary>
    /// <param name="task"></param>
    public void RemoveTaskFromQueue(TrangaTask task)
    {
        task.lastExecuted = DateTime.Now;
        foreach (List<TrangaTask> taskList in this._taskQueue.Values)
            taskList.Remove(task);
        task.state = TrangaTask.ExecutionState.Waiting;
    }

    /// <summary>
    /// Sets last execution time to start of time
    /// Let taskManager handle enqueuing
    /// </summary>
    /// <param name="task"></param>
    public void AddTaskToQueue(TrangaTask task)
    {
        task.lastExecuted = DateTime.UnixEpoch;
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
    public Connector GetConnector(string? connectorName)
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
        ExportData();
        
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
    /// <param name="importFilePath">working directory, filename has to be data.json</param>
    public static SettingsData LoadData(string importFilePath)
    {
        if (!File.Exists(importFilePath))
            return new SettingsData(Directory.GetCurrentDirectory(), null, null, new HashSet<TrangaTask>());

        string toRead = File.ReadAllText(importFilePath);
        SettingsData data = JsonConvert.DeserializeObject<SettingsData>(toRead)!;

        return data;
    }

    /// <summary>
    /// Exports data (settings, tasks) to file
    /// </summary>
    private void ExportData()
    {
        logger?.WriteLine(this.GetType().ToString(), $"Exporting data to {settings.settingsFilePath}");

        string serializedData = JsonConvert.SerializeObject(settings);
        File.WriteAllText(settings.settingsFilePath, serializedData);
    }

    public class SettingsData
    {
        public string downloadLocation { get; set; }
        public string settingsFilePath { get; }
        public Komga? komga { get; set; }
        public HashSet<TrangaTask> allTasks { get; }

        public SettingsData(string downloadLocation, string? settingsFilePath, Komga? komga, HashSet<TrangaTask> allTasks)
        {
            this.settingsFilePath = settingsFilePath ??
                                    Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                        "Tranga", "data.json");
            this.downloadLocation = downloadLocation;
            this.komga = komga;
            this.allTasks = allTasks;
        }
    }
}