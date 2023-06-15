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

    /// <param name="downloadFolderPath">Local path to save data (Manga) to</param>
    /// <param name="workingDirectory">Path to the working directory</param>
    /// <param name="imageCachePath">Path to the cover-image cache</param>
    /// <param name="libraryManagers"></param>
    /// <param name="notificationManagers"></param>
    /// <param name="logger"></param>
    public TaskManager(string downloadFolderPath, string workingDirectory, string imageCachePath, HashSet<LibraryManager> libraryManagers, HashSet<NotificationManager> notificationManagers, Logger? logger = null) : this(new TrangaSettings(downloadFolderPath, workingDirectory, libraryManagers, notificationManagers), logger)
    {
        
    }

    public TaskManager(TrangaSettings settings, Logger? logger = null)
    {
        this.logger = logger;
        this._connectors = new Connector[]
        {
            new MangaDex(settings.downloadLocation, settings.coverImageCache, logger),
            new Manganato(settings.downloadLocation, settings.coverImageCache, logger),
            new Mangasee(settings.downloadLocation, settings.coverImageCache, logger)
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
        int allTasksWaitingLength = _allTasks.Count(task => task.state is TrangaTask.ExecutionState.Waiting);
        while (_continueRunning)
        {
            TrangaTask[] tmp = _allTasks.Where(taskQuery =>
                    taskQuery.nextExecution < DateTime.Now &&
                    taskQuery.state is TrangaTask.ExecutionState.Waiting or TrangaTask.ExecutionState.Enqueued)
                .OrderBy(tmpTask => tmpTask.nextExecution).ToArray();
            foreach (TrangaTask task in tmp)
            {
                task.state = TrangaTask.ExecutionState.Enqueued;
                switch (task.task)
                {
                    case TrangaTask.Task.DownloadNewChapters:
                        if (!_allTasks.Any(taskQuery => taskQuery.task == TrangaTask.Task.DownloadNewChapters &&
                                                        taskQuery.state is TrangaTask.ExecutionState.Running &&
                                                        ((DownloadNewChaptersTask)taskQuery).connectorName == ((DownloadNewChaptersTask)task).connectorName))
                        {
                            ExecuteTaskNow(task);
                        }
                        break;
                    case TrangaTask.Task.DownloadChapter:
                        if (!_allTasks.Any(taskQuery =>
                                taskQuery.task == TrangaTask.Task.DownloadChapter &&
                                taskQuery.state is TrangaTask.ExecutionState.Running &&
                                ((DownloadChapterTask)taskQuery).connectorName ==
                                ((DownloadChapterTask)task).connectorName))
                        {
                            ExecuteTaskNow(task);
                        }
                        break;
                    case TrangaTask.Task.UpdateLibraries:
                        ExecuteTaskNow(task);
                        break;
                }
            }

            HashSet<DownloadChapterTask> toRemove = new();
            foreach (KeyValuePair<DownloadChapterTask, CancellationTokenSource> removeTask in _runningDownloadChapterTasks)
            {
                if (removeTask.Key.GetType() == typeof(DownloadChapterTask) &&
                    DateTime.Now.Subtract(removeTask.Key.lastChange) > TimeSpan.FromMinutes(3))//3 Minutes since last update to task -> remove
                {
                    logger?.WriteLine(this.GetType().ToString(), $"Disposing failed task {removeTask.Key}.");
                    removeTask.Value.Cancel();
                    toRemove.Add(removeTask.Key);
                }
            }
            foreach (DownloadChapterTask taskToRemove in toRemove)
            {
                DeleteTask(taskToRemove);
                DownloadChapterTask newTask = new (taskToRemove.task, taskToRemove.connectorName,
                    taskToRemove.publication, taskToRemove.chapter, taskToRemove.language,
                    (DownloadNewChaptersTask?)taskToRemove.parentTask);
                AddTask(newTask);
                taskToRemove.parentTask?.ReplaceFailedChildTask(taskToRemove, newTask);
            }
            
            if(allTasksWaitingLength != _allTasks.Count(task => task.state is TrangaTask.ExecutionState.Waiting))
                ExportDataAndSettings();
            allTasksWaitingLength = _allTasks.Count(task => task.state is TrangaTask.ExecutionState.Waiting);
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
        if(task.GetType() == typeof(DownloadChapterTask))
            _runningDownloadChapterTasks.Add((DownloadChapterTask)task, cToken);
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
            case TrangaTask.Task.DownloadNewChapters:
                IEnumerable<TrangaTask> matchingdnc =
                    _allTasks.Where(mTask => mTask.GetType() == typeof(DownloadNewChaptersTask));
                if (!matchingdnc.Any(mTask =>
                        ((DownloadNewChaptersTask)mTask).publication.internalId == ((DownloadNewChaptersTask)newTask).publication.internalId &&
                        ((DownloadNewChaptersTask)mTask).connectorName == ((DownloadNewChaptersTask)newTask).connectorName))
                    _allTasks.Add(newTask);
                else
                    logger?.WriteLine(this.GetType().ToString(), $"Task already exists {newTask}");
                break;
            case TrangaTask.Task.DownloadChapter:
                IEnumerable<TrangaTask> matchingdc =
                    _allTasks.Where(mTask => mTask.GetType() == typeof(DownloadChapterTask));
                if (!matchingdc.Any(mTask =>
                        ((DownloadChapterTask)mTask).publication.internalId == ((DownloadChapterTask)newTask).publication.internalId &&
                        ((DownloadChapterTask)mTask).connectorName == ((DownloadChapterTask)newTask).connectorName &&
                        ((DownloadChapterTask)mTask).chapter.sortNumber == ((DownloadChapterTask)newTask).chapter.sortNumber))
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
        if (removeTask.parentTask is not null)
            removeTask.parentTask.RemoveChildTask(removeTask);
        if (removeTask.GetType() == typeof(DownloadChapterTask) && _runningDownloadChapterTasks.ContainsKey((DownloadChapterTask)removeTask))
        {
            _runningDownloadChapterTasks[(DownloadChapterTask)removeTask].Cancel();
            _runningDownloadChapterTasks.Remove((DownloadChapterTask)removeTask);
        }
    }

    public TrangaTask? AddTask(TrangaTask.Task taskType, string? connectorName, string? internalId,
        TimeSpan reoccurrenceTime, string? language = "en")
    {
        TrangaTask? newTask = null;
        switch (taskType)
        {
            case TrangaTask.Task.UpdateLibraries:
                newTask = new UpdateLibrariesTask(taskType, reoccurrenceTime);
                break;
            case TrangaTask.Task.DownloadNewChapters:
                if (connectorName is null)
                    logger?.WriteLine(this.GetType().ToString(), $"Value connectorName can not be null.");
                if(internalId is null)
                    logger?.WriteLine(this.GetType().ToString(), $"Value internalId can not be null.");
                if(language is null)
                    logger?.WriteLine(this.GetType().ToString(), $"Value language can not be null.");
                if (connectorName is null || internalId is null || language is null)
                    return null;
                GetConnector(connectorName); //Check if connectorName is valid
                Publication publication = GetAllPublications().First(pub => pub.internalId == internalId);
                newTask = new DownloadNewChaptersTask(taskType, connectorName!, publication, reoccurrenceTime, language!);
                break;
        }
        if(newTask is not null)
            AddTask(newTask);
        return newTask;
    }

    /// <summary>
    /// Removes Task from task-collection
    /// </summary>
    /// <param name="task">TrangaTask.Task type</param>
    /// <param name="connectorName">Name of Connector that was used</param>
    /// <param name="publicationId">Publication that was used</param>
    public void DeleteTask(TrangaTask.Task task, string? connectorName, string? publicationId)
    {
        logger?.WriteLine(this.GetType().ToString(), $"Removing Task {task} {publicationId}");
        
        switch (task)
        {
            case TrangaTask.Task.UpdateLibraries:
                //Only one UpdateKomgaLibrary Task
                logger?.WriteLine(this.GetType().ToString(), $"Removing old {task}-Task.");
                _allTasks.RemoveWhere(trangaTask => trangaTask.task is TrangaTask.Task.UpdateLibraries);
                break;
            case TrangaTask.Task.DownloadNewChapters:
                if (connectorName is null || publicationId is null)
                    logger?.WriteLine(this.GetType().ToString(), "connectorName and publication can not be null");
                else
                {
                    _allTasks.RemoveWhere(mTask =>
                        mTask.GetType() == typeof(DownloadNewChaptersTask) &&
                        ((DownloadNewChaptersTask)mTask).publication.internalId == publicationId &&
                        ((DownloadNewChaptersTask)mTask).connectorName == connectorName);
                    foreach(TrangaTask rTask in _allTasks.Where(mTask =>
                                mTask.GetType() == typeof(DownloadChapterTask) &&
                                ((DownloadChapterTask)mTask).publication.internalId == publicationId &&
                                ((DownloadChapterTask)mTask).connectorName == connectorName))
                        DeleteTask(rTask);
                }
                break;
        }
        ExportDataAndSettings();
    }

    public IEnumerable<TrangaTask> GetTasksMatching(TrangaTask.Task taskType, string? connectorName = null, string? searchString = null, string? internalId = null, string? chapterSortNumber = null)
    {
        switch (taskType)
        {
            case TrangaTask.Task.UpdateLibraries:
                return _allTasks.Where(tTask => tTask.task == TrangaTask.Task.UpdateLibraries);
            case TrangaTask.Task.DownloadNewChapters:
                if(connectorName is null)
                    return _allTasks.Where(tTask => tTask.task == taskType);
                GetConnector(connectorName);//Name check
                IEnumerable<TrangaTask> matchingdnc = _allTasks.Where(tTask => tTask.GetType() == typeof(DownloadNewChaptersTask));
                if (searchString is not null)
                {
                    return matchingdnc.Where(mTask =>
                        ((DownloadNewChaptersTask)mTask).connectorName == connectorName &&
                        ((DownloadNewChaptersTask)mTask).ToString().Contains(searchString, StringComparison.InvariantCultureIgnoreCase));
                }
                else if (internalId is not null)
                {
                    return matchingdnc.Where(mTask =>
                        ((DownloadNewChaptersTask)mTask).connectorName == connectorName &&
                        ((DownloadNewChaptersTask)mTask).publication.internalId == internalId);
                }
                else
                    return _allTasks.Where(tTask =>
                        tTask.GetType() == typeof(DownloadNewChaptersTask) &&
                        ((DownloadNewChaptersTask)tTask).connectorName == connectorName);
                
            case TrangaTask.Task.DownloadChapter:
                if(connectorName is null)
                    return _allTasks.Where(tTask => tTask.task == taskType);
                GetConnector(connectorName);//Name check
                IEnumerable<TrangaTask> matchingdc = _allTasks.Where(tTask => tTask.GetType() == typeof(DownloadChapterTask));
                if (searchString is not null)
                {
                    return matchingdc.Where(mTask =>
                        ((DownloadChapterTask)mTask).connectorName == connectorName &&
                        ((DownloadChapterTask)mTask).ToString().Contains(searchString, StringComparison.InvariantCultureIgnoreCase));
                }
                else if (internalId is not null && chapterSortNumber is not null)
                {
                    return matchingdc.Where(mTask =>
                        ((DownloadChapterTask)mTask).connectorName == connectorName &&
                        ((DownloadChapterTask)mTask).publication.publicationId == internalId &&
                        ((DownloadChapterTask)mTask).chapter.sortNumber == chapterSortNumber);
                }
                else
                    return _allTasks.Where(tTask =>
                        tTask.GetType() == typeof(DownloadChapterTask) &&
                        ((DownloadChapterTask)tTask).connectorName == connectorName);
                
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

        foreach (TrangaTask task in this._allTasks.Where(tTask => tTask.parentTaskId is not null))
        {
            TrangaTask? parentTask = this._allTasks.FirstOrDefault(pTask => pTask.taskId == task.parentTaskId);
            if (parentTask is not null)
            {
                task.parentTask = parentTask;
                parentTask.AddChildTask(task);
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
        while(IsFileInUse(settings.settingsFilePath))
            Thread.Sleep(50);
        File.WriteAllText(settings.settingsFilePath, JsonConvert.SerializeObject(settings));
        
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