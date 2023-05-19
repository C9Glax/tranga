using Newtonsoft.Json;
using Tranga.Connectors;

namespace Tranga;

public class TaskManager
{
    private readonly Dictionary<Publication, List<Chapter>> _chapterCollection;
    private readonly HashSet<TrangaTask> _allTasks;
    private bool _continueRunning = true;
    private readonly Connector[] connectors;
    private readonly string folderPath;

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
                task.lastExecuted = DateTime.Now;
            }
            Thread.Sleep(1000);
        }
    }

    public bool AddTask(TrangaTask.Task task, Connector connector, Publication? publication, TimeSpan reoccurrence,
        string language = "")
    {
        if(!_allTasks.Any(trangaTask => trangaTask.task != task && trangaTask.connectorName != connector.name && trangaTask.publication?.downloadUrl != publication?.downloadUrl))
            return _allTasks.Add(new TrangaTask(connector.name, task, publication, reoccurrence, language));
        return false;
    }

    public bool RemoveTask(TrangaTask.Task task, string connectorName, Publication? publication)
    {
        return (_allTasks.RemoveWhere(trangaTask =>
            trangaTask.task == task && trangaTask.connectorName == connectorName && trangaTask.publication?.downloadUrl == publication?.downloadUrl)
            > 0);
    }

    public Dictionary<string, Connector> GetAvailableConnectors()
    {
        return this.connectors.ToDictionary(connector => connector.name, connector => connector);
    }

    public TrangaTask[] GetAllTasks()
    {
        TrangaTask[] ret = new TrangaTask[_allTasks.Count];
        _allTasks.CopyTo(ret);
        return ret;
    }
    
    public void Shutdown()
    {
        _continueRunning = false;
        ExportTasks(Directory.GetCurrentDirectory());
    }

    public HashSet<TrangaTask> ImportTasks(string importFolderPath)
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

    public void ExportTasks(string exportFolderPath)
    {
        string filePath = Path.Join(exportFolderPath, "tasks.json");
        string toWrite = JsonConvert.SerializeObject(_allTasks.ToArray());
        File.WriteAllText(filePath,toWrite);
    }
}