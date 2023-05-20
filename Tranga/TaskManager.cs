﻿using Newtonsoft.Json;
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
    private readonly Connector[] _connectors;
    private string downloadLocation { get; }
    private string? komgaBaseUrl { get; }

    /// <param name="folderPath">Local path to save data (Manga) to</param>
    /// <param name="komgaBaseUrl">The Url of the Komga-instance that you want to update</param>
    public TaskManager(string folderPath, string? komgaBaseUrl = null)
    {
        this._connectors = new Connector[]{ new MangaDex(folderPath) };
        _chapterCollection = new();
        _allTasks = new HashSet<TrangaTask>();
        
        this.downloadLocation = folderPath;
        this.komgaBaseUrl = komgaBaseUrl;
        
        Thread taskChecker = new(TaskCheckerThread);
        taskChecker.Start();
    }

    public TaskManager(SettingsData settings)
    {
        this._connectors = new Connector[]{ new MangaDex(settings.downloadLocation) };
        _chapterCollection = new();
        this.downloadLocation = settings.downloadLocation;
        this.komgaBaseUrl = settings.komgaUrl;
        _allTasks = settings.allTasks;
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
                    TaskExecutor.Execute(this._connectors, task, this._chapterCollection); //Might crash here, when adding new Task while another Task is running. Check later
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
            TaskExecutor.Execute(this._connectors, task, this._chapterCollection);
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
    public TrangaTask AddTask(TrangaTask.Task task, string connectorName, Publication? publication, TimeSpan reoccurrence,
        string language = "")
    {
        //Get appropriate Connector from available Connectors for TrangaTask
        Connector? connector = _connectors.FirstOrDefault(c => c.name == connectorName);
        if (connector is null)
            throw new ArgumentException($"Connector {connectorName} is not a known connector.");
        
        TrangaTask newTask = new TrangaTask(connector.name, task, publication, reoccurrence, language);
        //Check if same task already exists
        if (!_allTasks.Any(trangaTask => trangaTask.task != task && trangaTask.connectorName != connector.name &&
                                         trangaTask.publication?.downloadUrl != publication?.downloadUrl))
        {
            if(task != TrangaTask.Task.UpdatePublications)
                _chapterCollection.Add((Publication)publication!, new List<Chapter>());
            _allTasks.Add(newTask);
            ExportData(Directory.GetCurrentDirectory());
        }
        return newTask;
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
    /// Shuts down the taskManager.
    /// </summary>
    /// <param name="force">If force is true, tasks are aborted.</param>
    public void Shutdown(bool force = false)
    {
        _continueRunning = false;
        ExportData(Directory.GetCurrentDirectory());
        
        if(force)
            Environment.Exit(_allTasks.Count(task => task.isBeingExecuted));
        
        //Wait for tasks to finish
        while(_allTasks.Any(task => task.isBeingExecuted))
            Thread.Sleep(10);
        Environment.Exit(0);
    }

    public static SettingsData ImportData(string importFolderPath)
    {
        string importPath = Path.Join(importFolderPath, "data.json");
        if (!File.Exists(importPath))
            return new SettingsData("", null, new HashSet<TrangaTask>());

        string toRead = File.ReadAllText(importPath);
        SettingsData data = JsonConvert.DeserializeObject<SettingsData>(toRead)!;

        return data;
    }

    private void ExportData(string exportFolderPath)
    {
        SettingsData data = new SettingsData(this.downloadLocation, this.komgaBaseUrl, this._allTasks);

        string exportPath = Path.Join(exportFolderPath, "data.json");
        string serializedData = JsonConvert.SerializeObject(data);
        File.WriteAllText(exportPath, serializedData);
    }

    public class SettingsData
    {
        public string downloadLocation { get; }
        public string? komgaUrl { get; }
        public HashSet<TrangaTask> allTasks { get; }

        public SettingsData(string downloadLocation, string? komgaUrl, HashSet<TrangaTask> allTasks)
        {
            this.downloadLocation = downloadLocation;
            this.komgaUrl = komgaUrl;
            this.allTasks = allTasks;
        }
    }
}