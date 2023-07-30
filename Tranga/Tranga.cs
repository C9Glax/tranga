using System.Globalization;
using System.Runtime.InteropServices;
using Logging;
using Tranga.API;
using Tranga.Connectors;
using Tranga.NotificationManagers;
using Tranga.TrangaTasks;

namespace Tranga;

public static class Tranga
{
    public static void Main(string[] args)
    {
        string applicationFolderPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Tranga-API");
        string downloadFolderPath = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/Manga" : Path.Join(applicationFolderPath, "Manga");
        string logsFolderPath = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/var/log/Tranga" : Path.Join(applicationFolderPath, "log");
        string logFilePath = Path.Join(logsFolderPath, $"log-{DateTime.Now:dd-M-yyyy-HH-mm-ss}.txt");
        string settingsFilePath = Path.Join(applicationFolderPath, "settings.json");
        
        
        Directory.CreateDirectory(logsFolderPath);
        Logger logger;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            logger =new(new[] { Logger.LoggerType.FileLogger, Logger.LoggerType.ConsoleLogger }, Console.Out, Console.Out.Encoding, logFilePath);
        else
            logger = new(new[] { Logger.LoggerType.FileLogger }, Console.Out, Console.Out.Encoding, logFilePath);
        
        logger.WriteLine("Tranga",value: "\n"+
                                         "-------------------------------------------\n"+
                                         " Starting Tranga-API\n"+
                                         "-------------------------------------------");
        logger.WriteLine("Tranga", "Loading settings.");

        TrangaSettings settings = File.Exists(settingsFilePath)
            ? TrangaSettings.LoadSettings(settingsFilePath)
            : new TrangaSettings(downloadFolderPath, applicationFolderPath);

        Directory.CreateDirectory(settings.workingDirectory);
        Directory.CreateDirectory(settings.downloadLocation);
        Directory.CreateDirectory(settings.coverImageCache);

        logger.WriteLine("Tranga",$"Application-Folder: {settings.workingDirectory}");
        logger.WriteLine("Tranga",$"Settings-File-Path: {settings.settingsFilePath}");
        logger.WriteLine("Tranga",$"Download-Folder-Path: {settings.downloadLocation}");
        logger.WriteLine("Tranga",$"Logfile-Path: {logFilePath}");
        logger.WriteLine("Tranga",$"Image-Cache-Path: {settings.coverImageCache}");

        logger.WriteLine("Tranga", "Loading Taskmanager.");
        TaskManager taskManager = new (settings, logger);
        
        Server _ = new (6531, taskManager);
        foreach(NotificationManager nm in taskManager.commonObjects.notificationManagers)
            nm.SendNotification("Tranga-API", "Started Tranga-API");
        
        if(!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            TaskMode(taskManager, logger);
    }
    
    private static void TaskMode(TaskManager taskManager, Logger logger)
    {
        ConsoleKey selection = ConsoleKey.EraseEndOfFile;
        PrintMenu(taskManager, taskManager.settings.downloadLocation);
        while (selection != ConsoleKey.Q)
        {
            int taskCount = taskManager.GetAllTasks().Length;
            int taskRunningCount = taskManager.GetAllTasks().Count(task => task.state == TrangaTask.ExecutionState.Running);
            int taskEnqueuedCount =
                taskManager.GetAllTasks().Count(task => task.state == TrangaTask.ExecutionState.Enqueued);
            Console.SetCursorPosition(0,1);
            Console.WriteLine($"Tasks (Running/Queue/Total)): {taskRunningCount}/{taskEnqueuedCount}/{taskCount}");

            if (Console.KeyAvailable)
            {
                selection = Console.ReadKey().Key;
                switch (selection)
                {
                    case ConsoleKey.L:
                        while (!Console.KeyAvailable)
                        {
                            PrintTasks(taskManager.GetAllTasks(), logger);
                            Console.WriteLine("Press any key.");
                            Thread.Sleep(500);
                        }
                        Console.ReadKey();
                        break;
                    case ConsoleKey.C:
                        CreateTask(taskManager);
                        Console.WriteLine("Press any key.");
                        Console.ReadKey();
                        break;
                    case ConsoleKey.D:
                        DeleteTask(taskManager);
                        Console.WriteLine("Press any key.");
                        Console.ReadKey();
                        break;
                    case ConsoleKey.E:
                        ExecuteTaskNow(taskManager);
                        Console.WriteLine("Press any key.");
                        Console.ReadKey();
                        break;
                    case ConsoleKey.S:
                        SearchTasks(taskManager);
                        Console.WriteLine("Press any key.");
                        Console.ReadKey();
                        break;
                    case ConsoleKey.R:
                        while (!Console.KeyAvailable)
                        {
                            PrintTasks(
                                taskManager.GetAllTasks().Where(eTask => eTask.state == TrangaTask.ExecutionState.Running)
                                    .ToArray(), logger);
                            Console.WriteLine("Press any key.");
                            Thread.Sleep(500);
                        }
                        Console.ReadKey();
                        break;
                    case ConsoleKey.K:
                        while (!Console.KeyAvailable)
                        {
                            PrintTasks(
                                taskManager.GetAllTasks()
                                    .Where(qTask => qTask.state is TrangaTask.ExecutionState.Enqueued)
                                    .ToArray(), logger);
                            Console.WriteLine("Press any key.");
                            Thread.Sleep(500);
                        }
                        Console.ReadKey();
                        break;
                    case ConsoleKey.F:
                        TailLog(logger);
                        Console.ReadKey();
                        break;
                    case ConsoleKey.G:
                        RemoveTaskFromQueue(taskManager, logger);
                        Console.WriteLine("Press any key.");
                        Console.ReadKey();
                        break;
                    case ConsoleKey.B:
                        AddTaskToQueue(taskManager, logger);
                        Console.WriteLine("Press any key.");
                        Console.ReadKey();
                        break;
                    case ConsoleKey.M:
                        AddMangaTaskToQueue(taskManager, logger);
                        Console.WriteLine("Press any key.");
                        Console.ReadKey();
                        break; 
                }
                PrintMenu(taskManager, taskManager.settings.downloadLocation);
            }
            Thread.Sleep(200);
        }

        logger.WriteLine("Tranga_CLI", "Exiting.");
        Console.Clear();
        Console.WriteLine("Exiting.");
        if (taskManager.GetAllTasks().Any(task => task.state == TrangaTask.ExecutionState.Running))
        {
            Console.WriteLine("Force quit (Even with running tasks?) y/N");
            selection = Console.ReadKey().Key;
            while(selection != ConsoleKey.Y && selection != ConsoleKey.N)
                selection = Console.ReadKey().Key;
            taskManager.Shutdown(selection == ConsoleKey.Y);
        }else
            // ReSharper disable once RedundantArgumentDefaultValue Better readability
            taskManager.Shutdown(false);
    }
    
    private static void PrintMenu(TaskManager taskManager, string folderPath)
    {
        int taskCount = taskManager.GetAllTasks().Length;
        int taskRunningCount = taskManager.GetAllTasks().Count(task => task.state == TrangaTask.ExecutionState.Running);
        int taskEnqueuedCount =
            taskManager.GetAllTasks().Count(task => task.state == TrangaTask.ExecutionState.Enqueued);
        Console.Clear();
        Console.WriteLine($"Download Folder: {folderPath}");
        Console.WriteLine($"Tasks (Running/Queue/Total)): {taskRunningCount}/{taskEnqueuedCount}/{taskCount}");
        Console.WriteLine();
        Console.WriteLine($"{"C: Create Task",-30}{"L: List tasks",-30}{"B: Enqueue Task", -30}");
        Console.WriteLine($"{"D: Delete Task",-30}{"S: Search Tasks", -30}{"K: List Task Queue", -30}");
        Console.WriteLine($"{"E: Execute Task now",-30}{"R: List Running Tasks", -30}{"G: Remove Task from Queue", -30}");
        Console.WriteLine($"{"M: New Download Manga Task",-30}{"", -30}{"", -30}");
        Console.WriteLine($"{"",-30}{"F: Show Log",-30}{"Q: Exit",-30}");
    }
    
    private static void PrintTasks(TrangaTask[] tasks, Logger? logger)
    {
        logger?.WriteLine("Tranga_CLI", "Printing Tasks");
        int taskCount = tasks.Length;
        int taskRunningCount = tasks.Count(task => task.state == TrangaTask.ExecutionState.Running);
        int taskEnqueuedCount = tasks.Count(task => task.state == TrangaTask.ExecutionState.Enqueued);
        Console.Clear();
        int tIndex = 0;
        Console.WriteLine($"Tasks (Running/Queue/Total): {taskRunningCount}/{taskEnqueuedCount}/{taskCount}");
        string header =
            $"{"",-5}{"Task",-20} | {"Last Executed",-20} | {"Reoccurrence",-12} | {"State",-10} | {"Progress",-9} | {"Finished",-20} | {"Remaining",-12} | {"Connector",-15} | Publication/Manga ";
        Console.WriteLine(header);
        Console.WriteLine(new string('-', header.Length));
        foreach (TrangaTask trangaTask in tasks)
        {
            string[] taskSplit = trangaTask.ToString().Split(", ");
            Console.WriteLine($"{tIndex++:000}: {taskSplit[0],-20} | {taskSplit[1],-20} | {taskSplit[2],-12} | {taskSplit[3],-10} | {taskSplit[4],-9} | {taskSplit[5],-20} | {taskSplit[6][..12],-12} | {(taskSplit.Length > 7 ? taskSplit[7] : ""),-15} | {(taskSplit.Length > 8 ? taskSplit[8] : "")} {(taskSplit.Length > 9 ? taskSplit[9] : "")} {(taskSplit.Length > 10 ? taskSplit[10] : "")}");
        }
            
    }

    private static TrangaTask[] SelectTasks(TrangaTask[] tasks, Logger? logger)
    {
        logger?.WriteLine("Tranga_CLI", "Menu: Select task");
        if (tasks.Length < 1)
        {
            Console.Clear();
            Console.WriteLine("There are no available Tasks.");
            logger?.WriteLine("Tranga_CLI", "No available Tasks.");
            return Array.Empty<TrangaTask>();
        }
        PrintTasks(tasks, logger);
        
        logger?.WriteLine("Tranga_CLI", "Selecting Task to Remove (from queue)");
        Console.WriteLine("Enter q to abort");
        Console.WriteLine($"Select Task(s) (0-{tasks.Length - 1}):");

        string? selectedTask = Console.ReadLine();
        while(selectedTask is null || selectedTask.Length < 1)
            selectedTask = Console.ReadLine();
        
        if (selectedTask.Length == 1 && selectedTask.ToLower() == "q")
        {
            Console.Clear();
            Console.WriteLine("aborted.");
            logger?.WriteLine("Tranga_CLI", "aborted");
            return Array.Empty<TrangaTask>();
        }

        if (selectedTask.Contains('-'))
        {
            int start = Convert.ToInt32(selectedTask.Split('-')[0]);
            int end = Convert.ToInt32(selectedTask.Split('-')[1]);
            return tasks[start..end];
        }
        else
        {
            int selectedTaskIndex = Convert.ToInt32(selectedTask);
            return new[] { tasks[selectedTaskIndex] };
        }
    }
    
    private static void AddMangaTaskToQueue(TaskManager taskManager, Logger logger)
    {
        Console.Clear();
        logger.WriteLine("Tranga_CLI", "Menu: Add Manga Download to queue");
        
        Connector? connector = SelectConnector(taskManager.GetAvailableConnectors().Values.ToArray(), logger);
        if (connector is null)
            return;
                    
        Publication? publication = SelectPublication(taskManager, connector);
        if (publication is null)
            return;
        
        TimeSpan reoccurrence = SelectReoccurrence(logger);
        logger.WriteLine("Tranga_CLI", "Sending Task to TaskManager");
        TrangaTask nTask = new MonitorPublicationTask(connector.name, (Publication)publication, reoccurrence, "en");
        taskManager.AddTask(nTask);
        Console.WriteLine(nTask);
    }

    private static void AddTaskToQueue(TaskManager taskManager, Logger logger)
    {
        Console.Clear();
        logger.WriteLine("Tranga_CLI", "Menu: Add Task to queue");

        TrangaTask[] tasks = taskManager.GetAllTasks().Where(rTask =>
            rTask.state is not TrangaTask.ExecutionState.Enqueued and not TrangaTask.ExecutionState.Running).ToArray();
        
        TrangaTask[] selectedTasks = SelectTasks(tasks, logger);
        logger.WriteLine("Tranga_CLI", $"Sending {selectedTasks.Length} Tasks to TaskManager");
        foreach(TrangaTask task in selectedTasks)
            taskManager.AddTaskToQueue(task);
    }

    private static void RemoveTaskFromQueue(TaskManager taskManager, Logger logger)
    {
        Console.Clear();
        logger.WriteLine("Tranga_CLI", "Menu: Remove Task from queue");
        
        TrangaTask[] tasks = taskManager.GetAllTasks().Where(rTask => rTask.state is TrangaTask.ExecutionState.Enqueued).ToArray();

        TrangaTask[] selectedTasks = SelectTasks(tasks, logger);
        logger.WriteLine("Tranga_CLI", $"Sending {selectedTasks.Length} Tasks to TaskManager");
        foreach(TrangaTask task in selectedTasks)
            taskManager.RemoveTaskFromQueue(task);
    }

    private static void TailLog(Logger logger)
    {
        logger.WriteLine("Tranga_CLI", "Menu: Show Log-lines");
        Console.Clear();
        
        string[] lines = logger.Tail(20);
        foreach (string message in lines)
            Console.Write(message);

        while (!Console.KeyAvailable)
        {
            string[] newLines = logger.GetNewLines();
            foreach(string message in newLines)
                Console.Write(message);
            Thread.Sleep(40);
        }
    }

    private static void CreateTask(TaskManager taskManager)
    {
        taskManager.commonObjects.logger?.WriteLine("Tranga_CLI", "Menu: Creating Task");
        TrangaTask.Task? tmpTask = SelectTaskType(taskManager.commonObjects.logger);
        if (tmpTask is null)
            return;
        TrangaTask.Task task = (TrangaTask.Task)tmpTask;
        
        Connector? connector = null;
        if (task != TrangaTask.Task.UpdateLibraries)
        {
            connector = SelectConnector(taskManager.GetAvailableConnectors().Values.ToArray(), taskManager.commonObjects.logger);
            if (connector is null)
                return;
        }
                    
        Publication? publication = null;
        if (task != TrangaTask.Task.UpdateLibraries)
        {
            publication = SelectPublication(taskManager, connector!);
            if (publication is null)
                return;
        }

        if (task is TrangaTask.Task.MonitorPublication)
        {
            TimeSpan reoccurrence = SelectReoccurrence(taskManager.commonObjects.logger);
            taskManager.commonObjects.logger?.WriteLine("Tranga_CLI", "Sending Task to TaskManager");

            TrangaTask newTask = new MonitorPublicationTask(connector!.name, (Publication)publication!, reoccurrence, "en");
            taskManager.AddTask(newTask);
            Console.WriteLine(newTask);
        }else if (task is TrangaTask.Task.DownloadChapter)
        {
            foreach (Chapter chapter in SelectChapters(connector!, (Publication)publication!, taskManager.commonObjects.logger))
            {
                TrangaTask newTask = new DownloadChapterTask(connector!.name, (Publication)publication, chapter, "en");
                taskManager.AddTask(newTask);
                Console.WriteLine(newTask);
            }
        }
    }

    private static void ExecuteTaskNow(TaskManager taskManager)
    {
        taskManager.commonObjects.logger?.WriteLine("Tranga_CLI", "Menu: Executing Task");
        TrangaTask[] tasks = taskManager.GetAllTasks().Where(nTask => nTask.state is not TrangaTask.ExecutionState.Running).ToArray();
        
        TrangaTask[] selectedTasks = SelectTasks(tasks, taskManager.commonObjects.logger);
        taskManager.commonObjects.logger?.WriteLine("Tranga_CLI", $"Sending {selectedTasks.Length} Tasks to TaskManager");
        foreach(TrangaTask task in selectedTasks)
            taskManager.ExecuteTaskNow(task);
    }

    private static void DeleteTask(TaskManager taskManager)
    {
        taskManager.commonObjects.logger?.WriteLine("Tranga_CLI", "Menu: Delete Task");
        TrangaTask[] tasks = taskManager.GetAllTasks();
        
        TrangaTask[] selectedTasks = SelectTasks(tasks, taskManager.commonObjects.logger);
        taskManager.commonObjects.logger?.WriteLine("Tranga_CLI", $"Sending {selectedTasks.Length} Tasks to TaskManager");
        foreach(TrangaTask task in selectedTasks)
            taskManager.DeleteTask(task);
    }

    private static TrangaTask.Task? SelectTaskType(Logger? logger)
    {
        logger?.WriteLine("Tranga_CLI", "Menu: Select TaskType");
        Console.Clear();
        string[] taskNames = Enum.GetNames<TrangaTask.Task>();
        
        int tIndex = 0;
        Console.WriteLine("Available Tasks:");
        foreach (string taskName in taskNames)
            Console.WriteLine($"{tIndex++}: {taskName}");
        
        Console.WriteLine("Enter q to abort");
        Console.WriteLine($"Select Task (0-{taskNames.Length - 1}):");

        string? selectedTask = Console.ReadLine();
        while(selectedTask is null || selectedTask.Length < 1)
            selectedTask = Console.ReadLine();

        if (selectedTask.Length == 1 && selectedTask.ToLower() == "q")
        {
            Console.Clear();
            Console.WriteLine("aborted.");
            logger?.WriteLine("Tranga_CLI", "aborted.");
            return null;
        }
        
        try
        {
            int selectedTaskIndex = Convert.ToInt32(selectedTask);
            string selectedTaskName = taskNames[selectedTaskIndex];
            return Enum.Parse<TrangaTask.Task>(selectedTaskName);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Exception: {e.Message}");
            logger?.WriteLine("Tranga_CLI", e.Message);
        }

        return null;
    }

    private static TimeSpan SelectReoccurrence(Logger? logger)
    {
        logger?.WriteLine("Tranga_CLI", "Menu: Select Reoccurrence");
        Console.WriteLine("Select reoccurrence Timer (Format hh:mm:ss):");
        return TimeSpan.Parse(Console.ReadLine()!, new CultureInfo("en-US"));
    }

    private static Chapter[] SelectChapters(Connector connector, Publication publication, Logger? logger)
    {
        logger?.WriteLine("Tranga_CLI", "Menu: Select Chapters");
        Chapter[] availableChapters = connector.GetChapters(publication, "en");
        int cIndex = 0;
        Console.WriteLine("Chapters:");
        
        System.Text.StringBuilder sb = new();
        foreach(Chapter chapter in availableChapters)
        {
            sb.Append($"{cIndex++}: ");

            if(string.IsNullOrWhiteSpace(chapter.volumeNumber) == false)
            {
                sb.Append($"Vol.{chapter.volumeNumber} ");
            }

            if(string.IsNullOrWhiteSpace(chapter.chapterNumber) == false)
            {
                sb.Append($"Ch.{chapter.chapterNumber} ");
            }

            if(string.IsNullOrWhiteSpace(chapter.name) == false)
            {
                sb.Append($" - {chapter.name}");
            }

            Console.WriteLine(sb.ToString());
            sb.Clear();
        }
        
        Console.WriteLine("Enter q to abort");
        Console.WriteLine($"Select Chapter(s):");

        string? selectedChapters = Console.ReadLine();
        while(selectedChapters is null || selectedChapters.Length < 1)
            selectedChapters = Console.ReadLine();

        return connector.SelectChapters(publication, selectedChapters);
    }

    private static Connector? SelectConnector(Connector[] connectors, Logger? logger)
    {
        logger?.WriteLine("Tranga_CLI", "Menu: Select Connector");
        Console.Clear();
        
        int cIndex = 0;
        Console.WriteLine("Connectors:");
        foreach (Connector connector in connectors)
            Console.WriteLine($"{cIndex++}: {connector.name}");
        
        Console.WriteLine("Enter q to abort");
        Console.WriteLine($"Select Connector (0-{connectors.Length - 1}):");

        string? selectedConnector = Console.ReadLine();
        while(selectedConnector is null || selectedConnector.Length < 1)
            selectedConnector = Console.ReadLine();

        if (selectedConnector.Length == 1 && selectedConnector.ToLower() == "q")
        {
            Console.Clear();
            Console.WriteLine("aborted.");
            logger?.WriteLine("Tranga_CLI", "aborted.");
            return null;
        }
        
        try
        {
            int selectedConnectorIndex = Convert.ToInt32(selectedConnector);
            return connectors[selectedConnectorIndex];
        }
        catch (Exception e)
        {
            Console.WriteLine($"Exception: {e.Message}");
            logger?.WriteLine("Tranga_CLI", e.Message);
        }

        return null;
    }

    private static Publication? SelectPublication(TaskManager taskManager, Connector connector)
    {
        taskManager.commonObjects.logger?.WriteLine("Tranga_CLI", "Menu: Select Publication");
        
        Console.Clear();
        Console.WriteLine($"Connector: {connector.name}");
        Console.WriteLine("Publication search query (leave empty for all):");
        string? query = Console.ReadLine();

        Publication[] publications = connector.GetPublications(ref taskManager.collection, query ?? "");

        if (publications.Length < 1)
        {
            taskManager.commonObjects.logger?.WriteLine("Tranga_CLI", "No publications returned");
            Console.WriteLine($"No publications for query '{query}' returned;");
            return null;
        }
        
        int pIndex = 0;
        Console.WriteLine("Publications:");
        foreach(Publication publication in publications)
            Console.WriteLine($"{pIndex++}: {publication.sortName}");
        
        Console.WriteLine("Enter q to abort");
        Console.WriteLine($"Select publication to Download (0-{publications.Length - 1}):");

        string? selectedPublication = Console.ReadLine();
        while(selectedPublication is null || selectedPublication.Length < 1)
            selectedPublication = Console.ReadLine();

        if (selectedPublication.Length == 1 && selectedPublication.ToLower() == "q")
        {
            Console.Clear();
            Console.WriteLine("aborted.");
            taskManager.commonObjects.logger?.WriteLine("Tranga_CLI", "aborted.");
            return null;
        }
        
        try
        {
            int selectedPublicationIndex = Convert.ToInt32(selectedPublication);
            return publications[selectedPublicationIndex];
        }
        catch (Exception e)
        {
            Console.WriteLine($"Exception: {e.Message}");
            taskManager.commonObjects.logger?.WriteLine("Tranga_CLI", e.Message);
        }

        return null;
    }

    private static void SearchTasks(TaskManager taskManager)
    {
        taskManager.commonObjects.logger?.WriteLine("Tranga_CLI", "Menu: Search task");
        Console.Clear();
        Console.WriteLine("Enter search query:");
        string? query = Console.ReadLine();
        while (query is null || query.Length < 4)
            query = Console.ReadLine();
        PrintTasks(taskManager.GetAllTasks().Where(qTask =>
            qTask.ToString().ToLower().Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray(), taskManager.commonObjects.logger);
    }
}