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
    public HashSet<Publication> collection = new();
    private HashSet<TrangaTask> _allTasks = new();
    private readonly Dictionary<TrangaTask, CancellationTokenSource> _runningTasks = new ();
    private bool _continueRunning = true;
    private readonly Connector[] _connectors;
    public TrangaSettings settings { get; }
    public CommonObjects commonObjects { get; init; }

    public TaskManager(TrangaSettings settings, Logging.Logger? logger)
    {
        commonObjects = CommonObjects.LoadSettings(settings.settingsFilePath, logger);
        commonObjects.logger?.WriteLine("Tranga", value: "\n"+
                                                         @"-----------------------------------------------------------------"+"\n"+
                                                         @" |¯¯¯¯¯¯|°|¯¯¯¯¯¯\     /¯¯¯¯¯¯| |¯¯¯\|¯¯¯|  /¯¯¯¯¯¯\'   /¯¯¯¯¯¯| "+"\n"+
                                                         @" |      | |   x  <|'  /   !   | |       '| |   (/¯¯¯\° /   !   | "+ "\n"+
                                                         @"  ¯|__|¯  |__|\\__\\ /___/¯|_'| |___|\\__|  \\_____/' /___/¯|_'| "+ "\n"+
                                                         @"-----------------------------------------------------------------");
        this._connectors = new Connector[]
        {
            new MangaDex(settings, commonObjects),
            new Manganato(settings, commonObjects),
            new Mangasee(settings, commonObjects),
            new MangaKatana(settings, commonObjects)
        };
        
        this.settings = settings;
        Migrate.Files(settings);
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
        commonObjects.logger?.WriteLine(this.GetType().ToString(), "Starting TaskCheckerThread.");
        int waitingTasksCount = _allTasks.Count(task => task.state is TrangaTask.ExecutionState.Waiting);
        while (_continueRunning)
        {
            foreach (TrangaTask waitingButExecute in _allTasks.Where(taskQuery =>
                         taskQuery.nextExecution < DateTime.Now &&
                         taskQuery.state is TrangaTask.ExecutionState.Waiting))
            {
                waitingButExecute.state = TrangaTask.ExecutionState.Enqueued;
            }
                
            foreach (TrangaTask enqueuedTask in _allTasks.Where(enqueuedTask => enqueuedTask.state is TrangaTask.ExecutionState.Enqueued).OrderBy(enqueuedTask => enqueuedTask.nextExecution))
            {
                switch (enqueuedTask.task)
                {
                    case TrangaTask.Task.DownloadChapter:
                    case TrangaTask.Task.MonitorPublication:
                        if (!_allTasks.Any(taskQuery =>
                            {
                                if (taskQuery.state is not TrangaTask.ExecutionState.Running) return false;
                                switch (taskQuery)
                                {
                                    case DownloadChapterTask dct when enqueuedTask is DownloadChapterTask eDct && dct.connectorName == eDct.connectorName:
                                    case MonitorPublicationTask mpt when enqueuedTask is MonitorPublicationTask eMpt && mpt.connectorName == eMpt.connectorName:
                                        return true;
                                    default:
                                        return false;
                                }
                            }))
                        {
                            ExecuteTaskNow(enqueuedTask);
                        }
                        break;
                    case TrangaTask.Task.UpdateLibraries:
                        ExecuteTaskNow(enqueuedTask);
                        break;
                }
            }

            foreach (TrangaTask timedOutTask in _runningTasks.Keys
                         .Where(taskQuery => taskQuery.lastChange < DateTime.Now.Subtract(TimeSpan.FromMinutes(3))))
            {
                _runningTasks[timedOutTask].Cancel();
                timedOutTask.state = TrangaTask.ExecutionState.Failed;
            }

            foreach (TrangaTask finishedTask in _allTasks
                         .Where(taskQuery => taskQuery.state is TrangaTask.ExecutionState.Success).ToArray())
            {
                if(finishedTask is DownloadChapterTask)
                {
                    DeleteTask(finishedTask);
                    finishedTask.state = TrangaTask.ExecutionState.Success;
                }
                else
                {
                    finishedTask.state = TrangaTask.ExecutionState.Waiting;
                    this._runningTasks.Remove(finishedTask);
                }
            }
            
            foreach (TrangaTask failedTask in _allTasks.Where(taskQuery =>
                         taskQuery.state is TrangaTask.ExecutionState.Failed).ToArray())
            {
                DeleteTask(failedTask);
                TrangaTask newTask = failedTask.Clone();
                failedTask.parentTask?.AddChildTask(newTask);
                AddTask(newTask);
            }
            
            if(waitingTasksCount != _allTasks.Count(task => task.state is TrangaTask.ExecutionState.Waiting))
                ExportDataAndSettings();
            waitingTasksCount = _allTasks.Count(task => task.state is TrangaTask.ExecutionState.Waiting);
            Thread.Sleep(1000);
        }
    }

    /// <summary>
    /// Forces the execution of a given task
    /// </summary>
    /// <param name="task">Task to execute</param>
    public void ExecuteTaskNow(TrangaTask task)
    {
        task.state = TrangaTask.ExecutionState.Running;
        CancellationTokenSource cToken = new ();
        Task t = new(() =>
        {
            task.Execute(this, cToken.Token);
        }, cToken.Token);
        _runningTasks.Add(task, cToken);
        t.Start();
    }

    public void AddTask(TrangaTask newTask)
    {
        switch (newTask.task)
        {
            case TrangaTask.Task.UpdateLibraries:
                //Only one UpdateKomgaLibrary Task
                commonObjects.logger?.WriteLine(this.GetType().ToString(), $"Replacing old {newTask.task}-Task.");
                if (GetTasksMatching(newTask).FirstOrDefault() is { } exists)
                    _allTasks.Remove(exists);
                _allTasks.Add(newTask);
                ExportDataAndSettings();
                break;
            default:
                if (!GetTasksMatching(newTask).Any())
                {
                    commonObjects.logger?.WriteLine(this.GetType().ToString(), $"Adding new Task {newTask}");
                    _allTasks.Add(newTask);
                    ExportDataAndSettings();
                }
                else
                    commonObjects.logger?.WriteLine(this.GetType().ToString(), $"Task already exists {newTask}");
                break;
        }
    }

    public void DeleteTask(TrangaTask removeTask)
    {
        commonObjects.logger?.WriteLine(this.GetType().ToString(), $"Removing Task {removeTask}");
        if(_allTasks.Contains(removeTask))
            _allTasks.Remove(removeTask);
        removeTask.parentTask?.RemoveChildTask(removeTask);
        if (_runningTasks.ContainsKey(removeTask))
        {
            _runningTasks[removeTask].Cancel();
            _runningTasks.Remove(removeTask);
        }
        foreach(TrangaTask childTask in removeTask.childTasks)
            DeleteTask(childTask);
        ExportDataAndSettings();
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public IEnumerable<TrangaTask> GetTasksMatching(TrangaTask mTask)
    {
        switch (mTask.task)
        {
            case TrangaTask.Task.UpdateLibraries:
                return GetTasksMatching(TrangaTask.Task.UpdateLibraries);
            case TrangaTask.Task.DownloadChapter:
                DownloadChapterTask dct = (DownloadChapterTask)mTask;
                return GetTasksMatching(TrangaTask.Task.DownloadChapter, connectorName: dct.connectorName,
                    internalId: dct.publication.internalId, chapterNumber: dct.chapter.chapterNumber);
            case TrangaTask.Task.MonitorPublication:
                MonitorPublicationTask mpt = (MonitorPublicationTask)mTask;
                return GetTasksMatching(TrangaTask.Task.MonitorPublication, connectorName: mpt.connectorName,
                    internalId: mpt.publication.internalId);
        }
        return Array.Empty<TrangaTask>();
    }

    public IEnumerable<TrangaTask> GetTasksMatching(TrangaTask.Task taskType, string? connectorName = null, string? searchString = null, string? internalId = null, string? chapterNumber = null)
    {
        switch (taskType)
        {
            case TrangaTask.Task.UpdateLibraries:
                return _allTasks.Where(tTask => tTask.task == TrangaTask.Task.UpdateLibraries);
            case TrangaTask.Task.MonitorPublication:
                if(connectorName is null)
                    return _allTasks.Where(tTask => tTask.task == taskType);
                GetConnector(connectorName);//Name check
                if (searchString is not null)
                {
                    return _allTasks.Where(mTask =>
                        mTask is MonitorPublicationTask mpt && mpt.connectorName == connectorName &&
                        mpt.ToString().Contains(searchString, StringComparison.InvariantCultureIgnoreCase));
                }
                else if (internalId is not null)
                {
                    return _allTasks.Where(mTask =>
                        mTask is MonitorPublicationTask mpt && mpt.connectorName == connectorName &&
                        mpt.publication.internalId == internalId);
                }
                else
                    return _allTasks.Where(tTask =>
                        tTask is MonitorPublicationTask mpt && mpt.connectorName == connectorName);
                
            case TrangaTask.Task.DownloadChapter:
                if(connectorName is null)
                    return _allTasks.Where(tTask => tTask.task == taskType);
                GetConnector(connectorName);//Name check
                if (searchString is not null)
                {
                    return _allTasks.Where(mTask =>
                        mTask is DownloadChapterTask dct && dct.connectorName == connectorName &&
                        dct.ToString().Contains(searchString, StringComparison.InvariantCultureIgnoreCase));
                }
                else if (internalId is not null && chapterNumber is not null)
                {
                    return _allTasks.Where(mTask =>
                        mTask is DownloadChapterTask dct && dct.connectorName == connectorName &&
                        dct.publication.internalId == internalId &&
                        dct.chapter.chapterNumber == chapterNumber);
                }
                else
                    return _allTasks.Where(mTask =>
                        mTask is DownloadChapterTask dct && dct.connectorName == connectorName);
                
            default:
                return Array.Empty<TrangaTask>();
        }
    }

    /// <summary>
    /// Removes a Task from the queue
    /// </summary>
    /// <param name="task"></param>
    public void RemoveTaskFromQueue(TrangaTask task)
    {
        task.lastExecuted = DateTime.Now;
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
        return this.collection.ToArray();
    }

    public List<Chapter> GetExistingChaptersList(Connector connector, Publication publication, string language)
    {
        Chapter[] newChapters = connector.GetChapters(publication, language);
        return newChapters.Where(nChapter => nChapter.CheckChapterIsDownloaded(settings.downloadLocation)).ToList();
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
        commonObjects.logger?.WriteLine(this.GetType().ToString(), $"Shutting down (forced={force})");
        _continueRunning = false;
        ExportDataAndSettings();
        
        if(force)
            Environment.Exit(_allTasks.Count(task => task.state is TrangaTask.ExecutionState.Enqueued or TrangaTask.ExecutionState.Running));
        
        //Wait for tasks to finish
        while(_allTasks.Any(task => task.state is TrangaTask.ExecutionState.Running or TrangaTask.ExecutionState.Enqueued))
            Thread.Sleep(10);
        commonObjects.logger?.WriteLine(this.GetType().ToString(), "Tasks finished. Bye!");
        Environment.Exit(0);
    }

    private void ImportData()
    {
        commonObjects.logger?.WriteLine(this.GetType().ToString(), "Importing Data");
        if (File.Exists(settings.tasksFilePath))
        {
            commonObjects.logger?.WriteLine(this.GetType().ToString(), $"Importing tasks from {settings.tasksFilePath}");
            string buffer = File.ReadAllText(settings.tasksFilePath);
            this._allTasks = JsonConvert.DeserializeObject<HashSet<TrangaTask>>(buffer, new JsonSerializerSettings() { Converters = { new TrangaTask.TrangaTaskJsonConverter() } })!;
        }

        foreach (TrangaTask task in this._allTasks.Where(tTask => tTask.parentTaskId is not null).ToArray())
        {
            TrangaTask? parentTask = this._allTasks.FirstOrDefault(pTask => pTask.taskId == task.parentTaskId);
            if (parentTask is not null)
            {
                this.DeleteTask(task);
                parentTask.lastExecuted = DateTime.UnixEpoch;
            }
        }
    }

    /// <summary>
    /// Exports data (settings, tasks) to file
    /// </summary>
    private void ExportDataAndSettings()
    {
        commonObjects.logger?.WriteLine(this.GetType().ToString(), $"Exporting settings to {settings.settingsFilePath}");
        settings.ExportSettings();
        
        commonObjects.logger?.WriteLine(this.GetType().ToString(), $"Exporting tasks to {settings.tasksFilePath}");
        while(IsFileInUse(settings.tasksFilePath))
            Thread.Sleep(50);
        File.WriteAllText(settings.tasksFilePath, JsonConvert.SerializeObject(this._allTasks));
    }

    private bool IsFileInUse(string path)
    {
        if (!File.Exists(path))
            return false;
        try
        {
            using FileStream stream = new (path, FileMode.Open, FileAccess.Read, FileShare.None);
            stream.Close();
        }
        catch (IOException)
        {
            return true;
        }
        return false;
    }
}