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
    private HashSet<TrangaTask> _allTasks = new();
    private bool _continueRunning = true;
    private readonly Connector[] _connectors;
    public TrangaSettings settings { get; }
    private Logger? logger { get; }

    private readonly Dictionary<DownloadChapterTask, CancellationTokenSource> _runningDownloadChapterTasks = new();

    public TaskManager(TrangaSettings settings, Logger? logger = null)
    {
        this.logger = logger;
        this._connectors = new Connector[]
        {
            new MangaDex(settings, logger),
            new Manganato(settings, logger),
            new Mangasee(settings, logger),
            new MangaKatana(settings, logger)
        };
        
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
        int waitingTasksCount = _allTasks.Count(task => task.state is TrangaTask.ExecutionState.Waiting);
        while (_continueRunning)
        {
            foreach (TrangaTask waitingButExecute in _allTasks.Where(taskQuery =>
                         taskQuery.nextExecution < DateTime.Now &&
                         taskQuery.state is TrangaTask.ExecutionState.Waiting))
            {
                waitingButExecute.state = TrangaTask.ExecutionState.Enqueued;
            }
                
            foreach (TrangaTask enqueuedTask in _allTasks.Where(enqueuedTask => enqueuedTask.state is TrangaTask.ExecutionState.Enqueued))
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

            foreach (TrangaTask timedOutTask in _allTasks
                         .Where(taskQuery => taskQuery.lastChange < DateTime.Now.Subtract(TimeSpan.FromMinutes(3))))
            {
                if(timedOutTask is DownloadChapterTask dct)
                    _runningDownloadChapterTasks[dct].Cancel();
                timedOutTask.state = TrangaTask.ExecutionState.Failed;
            }
            
            foreach (TrangaTask failedDownloadChapterTask in _allTasks.Where(taskQuery =>
                         taskQuery.state is TrangaTask.ExecutionState.Failed && taskQuery is DownloadChapterTask).ToArray())
            {
                DeleteTask(failedDownloadChapterTask);
                TrangaTask newTask = failedDownloadChapterTask.Clone();
                failedDownloadChapterTask.parentTask?.AddChildTask(newTask);
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
            task.Execute(this, this.logger, cToken.Token);
        }, cToken.Token);
        if(task is DownloadChapterTask chapterTask)
            _runningDownloadChapterTasks.Add(chapterTask, cToken);
        t.Start();
    }

    public void AddTask(TrangaTask newTask)
    {
        logger?.WriteLine(this.GetType().ToString(), $"Adding new Task {newTask}");

        switch (newTask.task)
        {
            case TrangaTask.Task.UpdateLibraries:
                //Only one UpdateKomgaLibrary Task
                logger?.WriteLine(this.GetType().ToString(), $"Replacing old {newTask.task}-Task.");
                _allTasks.RemoveWhere(trangaTask => trangaTask.task is TrangaTask.Task.UpdateLibraries);
                _allTasks.Add(newTask);
                break;
            case TrangaTask.Task.MonitorPublication:
                MonitorPublicationTask mpt = (MonitorPublicationTask)newTask;
                if(!GetTasksMatching(mpt.task, mpt.connectorName, internalId:mpt.publication.internalId).Any())
                    _allTasks.Add(newTask);
                else
                    logger?.WriteLine(this.GetType().ToString(), $"Task already exists {newTask}");
                break;
            case TrangaTask.Task.DownloadChapter:
                DownloadChapterTask dct = (DownloadChapterTask)newTask;
                if(!GetTasksMatching(dct.task, dct.connectorName, internalId:dct.publication.internalId, chapterSortNumber:dct.chapter.sortNumber).Any())
                    _allTasks.Add(newTask);
                else
                    logger?.WriteLine(this.GetType().ToString(), $"Task already exists {newTask}");
                break;
        }
        ExportDataAndSettings();
    }

    public void DeleteTask(TrangaTask removeTask)
    {
        logger?.WriteLine(this.GetType().ToString(), $"Removing Task {removeTask}");
        _allTasks.Remove(removeTask);
        removeTask.parentTask?.RemoveChildTask(removeTask);
        if (removeTask is DownloadChapterTask cRemoveTask && _runningDownloadChapterTasks.ContainsKey(cRemoveTask))
        {
            _runningDownloadChapterTasks[cRemoveTask].Cancel();
            _runningDownloadChapterTasks.Remove(cRemoveTask);
        }
        foreach(TrangaTask childTask in removeTask.childTasks)
            DeleteTask(childTask);
    }

    public IEnumerable<TrangaTask> GetTasksMatching(TrangaTask.Task taskType, string? connectorName = null, string? searchString = null, string? internalId = null, string? chapterSortNumber = null)
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
                else if (internalId is not null && chapterSortNumber is not null)
                {
                    return _allTasks.Where(mTask =>
                        mTask is DownloadChapterTask dct && dct.connectorName == connectorName &&
                        dct.publication.internalId == internalId &&
                        dct.chapter.sortNumber == chapterSortNumber);
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

    public Publication[] GetPublicationsFromConnector(Connector connector, string? title = null)
    {
        Publication[] ret = connector.GetPublications(title ?? "");
        foreach (Publication publication in ret)
        {
            if(chapterCollection.All(pub => pub.Key.internalId != publication.internalId))
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
    /// Updates the available Chapters of a Publication
    /// </summary>
    /// <param name="connector">Connector to use</param>
    /// <param name="publication">Publication to check</param>
    /// <param name="language">Language to receive chapters for</param>
    /// <returns>List of Chapters that were previously not in collection</returns>
    public List<Chapter> GetNewChaptersList(Connector connector, Publication publication, string language)
    {
        List<Chapter> newChaptersList = new();
        chapterCollection.TryAdd(publication, newChaptersList); //To ensure publication is actually in collection
        
        Chapter[] newChapters = connector.GetChapters(publication, language);
        newChaptersList = newChapters.Where(nChapter => !connector.CheckChapterIsDownloaded(publication, nChapter)).ToList();
        
        return newChaptersList;
    }

    public List<Chapter> GetExistingChaptersList(Connector connector, Publication publication, string language)
    {
        Chapter[] newChapters = connector.GetChapters(publication, language);
        return newChapters.Where(nChapter => connector.CheckChapterIsDownloaded(publication, nChapter)).ToList();
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

        foreach (TrangaTask task in this._allTasks.Where(tTask => tTask.parentTaskId is not null).ToArray())
        {
            TrangaTask? parentTask = this._allTasks.FirstOrDefault(pTask => pTask.taskId == task.parentTaskId);
            if (parentTask is not null)
            {
                this.DeleteTask(task);
                parentTask.lastExecuted = DateTime.UnixEpoch;
            }
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
        settings.ExportSettings();
        
        logger?.WriteLine(this.GetType().ToString(), $"Exporting tasks to {settings.tasksFilePath}");
        while(IsFileInUse(settings.tasksFilePath))
            Thread.Sleep(50);
        File.WriteAllText(settings.tasksFilePath, JsonConvert.SerializeObject(this._allTasks));
        
        logger?.WriteLine(this.GetType().ToString(), $"Exporting known publications to {settings.knownPublicationsPath}");
        while(IsFileInUse(settings.knownPublicationsPath))
            Thread.Sleep(50);
        File.WriteAllText(settings.knownPublicationsPath, JsonConvert.SerializeObject(this.chapterCollection.Keys.ToArray()));
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