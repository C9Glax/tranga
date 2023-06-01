using Logging;
using Newtonsoft.Json;
using Tranga.Connectors;
using Tranga.TrangaTasks;

namespace Tranga;

/// <summary>
/// Manages all TrangaTasks.
/// Provides a Threaded environment to execute Tasks, and still manage the Task-Collection
/// </summary>
public class TaskManager
{
    public Dictionary<Publication, List<Chapter>> chapterCollection = new();
    private HashSet<TrangaTask> _allTasks;
    private bool _continueRunning = true;
    private readonly Connector[] _connectors;
    private readonly Dictionary<Connector, List<TrangaTask>> _taskQueue = new();
    public TrangaSettings settings { get; }
    private Logger? logger { get; }
    public Komga? komga => settings.komga;

    /// <param name="downloadFolderPath">Local path to save data (Manga) to</param>
    /// <param name="workingDirectory">Path to the working directory</param>
    /// <param name="imageCachePath">Path to the cover-image cache</param>
    /// <param name="komgaBaseUrl">The Url of the Komga-instance that you want to update</param>
    /// <param name="komgaUsername">The Komga username</param>
    /// <param name="komgaPassword">The Komga password</param>
    /// <param name="logger"></param>
    public TaskManager(string downloadFolderPath, string workingDirectory, string imageCachePath, string? komgaBaseUrl = null, string? komgaUsername = null, string? komgaPassword = null, Logger? logger = null)
    {
        this.logger = logger;
        _allTasks = new HashSet<TrangaTask>();

        Komga? newKomga = null;
        if (komgaBaseUrl != null && komgaUsername != null && komgaPassword != null)
            newKomga = new Komga(komgaBaseUrl, komgaUsername, komgaPassword, logger);
        
        this.settings = new TrangaSettings(downloadFolderPath, workingDirectory, newKomga);
        ExportDataAndSettings();
        
        this._connectors = new Connector[]
        {
            new MangaDex(downloadFolderPath, imageCachePath, logger),
            new Manganato(downloadFolderPath, imageCachePath, logger)
        };
        foreach(Connector cConnector in this._connectors)
            _taskQueue.Add(cConnector, new List<TrangaTask>());
        
        Thread taskChecker = new(TaskCheckerThread);
        taskChecker.Start();
    }

    public void UpdateSettings(string? downloadLocation, string? komgaUrl, string? komgaAuth)
    {
        if (komgaUrl is not null && komgaAuth is not null && komgaUrl.Length > 0 && komgaAuth.Length > 0)
            settings.komga = new Komga(komgaUrl, komgaAuth, null);
        if (downloadLocation is not null && downloadLocation.Length > 0)
            settings.downloadLocation = downloadLocation;
        ExportDataAndSettings();
    }

    public TaskManager(TrangaSettings settings, Logger? logger = null)
    {
        this.logger = logger;
        this._connectors = new Connector[]
        {
            new MangaDex(settings.downloadLocation, settings.coverImageCache, logger),
            new Manganato(settings.downloadLocation, settings.coverImageCache, logger)
        };
        foreach(Connector cConnector in this._connectors)
            _taskQueue.Add(cConnector, new List<TrangaTask>());
        _allTasks = new HashSet<TrangaTask>();
        
        this.settings = settings;
        ImportData();
        ExportDataAndSettings();
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
                    ExportDataAndSettings();
                
                if (connectorTaskQueue.Value.Count > 0 && connectorTaskQueue.Value.All(task => task.state is TrangaTask.ExecutionState.Enqueued))
                    ExecuteTaskNow(connectorTaskQueue.Value.First());
            }
            
            //Check if task should be executed
            //Depending on type execute immediately or enqueue
            foreach (TrangaTask task in _allTasks.Where(aTask => aTask.ShouldExecute()))
            {
                task.state = TrangaTask.ExecutionState.Enqueued;
                if(task.task == TrangaTask.Task.UpdateKomgaLibrary)
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
        
        Task t = new(() =>
        {
            task.Execute(this, this.logger);
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

        TrangaTask? newTask = null;
        if (task == TrangaTask.Task.UpdateKomgaLibrary)
        {
            newTask = new UpdateKomgaLibraryTask(task, reoccurrence);
            logger?.WriteLine(this.GetType().ToString(), $"Removing old {task}-Task.");
            //Only one UpdateKomgaLibrary Task
            _allTasks.RemoveWhere(trangaTask => trangaTask.task is TrangaTask.Task.UpdateKomgaLibrary);
            _allTasks.Add(newTask);
            logger?.WriteLine(this.GetType().ToString(), $"Added new Task {newTask}");
        }else if (task == TrangaTask.Task.DownloadNewChapters)
        {
            //Get appropriate Connector from available Connectors for TrangaTask
            Connector? connector = _connectors.FirstOrDefault(c => c.name == connectorName);
            if (connectorName is null)
                throw new ArgumentException($"connectorName can not be null for task {task}");

            if (publication is null)
                throw new ArgumentException($"publication can not be null for task {task}");
            Publication pub = (Publication)publication;
            newTask = new DownloadNewChaptersTask(task, connector!.name, pub, reoccurrence, language);

            if (!_allTasks.Any(trangaTask =>
                    trangaTask.task == task && trangaTask.publication?.internalId == pub.internalId &&
                    trangaTask.connectorName == connector.name))
            {
                _allTasks.Add(newTask);
                logger?.WriteLine(this.GetType().ToString(), $"Added new Task {newTask}");
            }
            else
                logger?.WriteLine(this.GetType().ToString(), $"Task already exists {newTask}");
        }
        ExportDataAndSettings();

        if (newTask is null)
            throw new Exception("Invalid path");
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
            logger?.WriteLine(this.GetType().ToString(), $"Removed Task {task} from all Tasks.");
        }
        else if (connectorName is null)
            throw new ArgumentException($"connectorName can not be null for Task {task}");
        else
        {
            foreach (List<TrangaTask> taskQueue in this._taskQueue.Values)
                if(taskQueue.RemoveAll(trangaTask =>
                       trangaTask.task == task && trangaTask.connectorName == connectorName &&
                       trangaTask.publication?.internalId == publication?.internalId) > 0)
                    logger?.WriteLine(this.GetType().ToString(), $"Removed Task {task} {publication?.sortName} {publication?.internalId} from Queue.");
                else
                    logger?.WriteLine(this.GetType().ToString(), $"Task {task} {publication?.sortName} {publication?.internalId} was not in Queue.");
            if(_allTasks.RemoveWhere(trangaTask =>
                trangaTask.task == task && trangaTask.connectorName == connectorName &&
                trangaTask.publication?.internalId == publication?.internalId) > 0)
                logger?.WriteLine(this.GetType().ToString(), $"Removed Task {task} {publication?.sortName} {publication?.internalId} from all Tasks.");
            else
                logger?.WriteLine(this.GetType().ToString(), $"No Task {task} {publication?.sortName} {publication?.internalId} could be found.");
        }
        ExportDataAndSettings();
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

    public Publication[] GetPublicationsFromConnector(Connector connector, string? title = null)
    {
        Publication[] ret = connector.GetPublications(title ?? "");
        foreach (Publication publication in ret)
        {
            if(!chapterCollection.Any(pub => pub.Key.sortName == publication.sortName))
                this.chapterCollection.TryAdd(publication, new List<Chapter>());
        }
        return ret;
    }
    
    /// <returns>All added Publications</returns>
    public Publication[] GetAllPublications()
    {
        return this.chapterCollection.Keys.ToArray();
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
        return ret;
    }
    
    /// <summary>
    /// Shuts down the taskManager.
    /// </summary>
    /// <param name="force">If force is true, tasks are aborted.</param>
    public void Shutdown(bool force = false)
    {
        logger?.WriteLine(this.GetType().ToString(), $"Shutting down (forced={force})");
        _continueRunning = false;
        ExportDataAndSettings();
        
        if(force)
            Environment.Exit(_allTasks.Count(task => task.state is TrangaTask.ExecutionState.Enqueued or TrangaTask.ExecutionState.Running));
        
        //Wait for tasks to finish
        while(_allTasks.Any(task => task.state is TrangaTask.ExecutionState.Running or TrangaTask.ExecutionState.Enqueued))
            Thread.Sleep(10);
        logger?.WriteLine(this.GetType().ToString(), "Tasks finished. Bye!");
        Environment.Exit(0);
    }

    private void ImportData()
    {
        logger?.WriteLine(this.GetType().ToString(), "Importing Data");
        string buffer;
        if (File.Exists(settings.tasksFilePath))
        {
            logger?.WriteLine(this.GetType().ToString(), $"Importing tasks from {settings.tasksFilePath}");
            buffer = File.ReadAllText(settings.tasksFilePath);
            this._allTasks = JsonConvert.DeserializeObject<HashSet<TrangaTask>>(buffer, new JsonSerializerSettings() { Converters = { new TrangaTask.TrangaTaskJsonConverter() } })!;
        }

        if (File.Exists(settings.knownPublicationsPath))
        {
            logger?.WriteLine(this.GetType().ToString(), $"Importing known publications from {settings.knownPublicationsPath}");
            buffer = File.ReadAllText(settings.knownPublicationsPath);
            Publication[] publications = JsonConvert.DeserializeObject<Publication[]>(buffer)!;
            foreach (Publication publication in publications)
                this.chapterCollection.TryAdd(publication, new List<Chapter>());
        }
    }

    /// <summary>
    /// Exports data (settings, tasks) to file
    /// </summary>
    private void ExportDataAndSettings()
    {
        logger?.WriteLine(this.GetType().ToString(), $"Exporting settings to {settings.settingsFilePath}");
        File.WriteAllText(settings.settingsFilePath, JsonConvert.SerializeObject(settings));
        
        logger?.WriteLine(this.GetType().ToString(), $"Exporting tasks to {settings.tasksFilePath}");
        File.WriteAllText(settings.tasksFilePath, JsonConvert.SerializeObject(this._allTasks));
        
        logger?.WriteLine(this.GetType().ToString(), $"Exporting known publications to {settings.knownPublicationsPath}");
        File.WriteAllText(settings.knownPublicationsPath, JsonConvert.SerializeObject(this.chapterCollection.Keys.ToArray()));
    }

    
}