using System.Globalization;
using Logging;
using Tranga;

namespace Tranga_CLI;

/*
 * This is written with pure hatred for readability.
 * At some point do this properly.
 * Read at own risk.
 */

public static class Tranga_Cli
{
    public static void Main(string[] args)
    {
        Logger logger = new(new[] { Logger.LoggerType.FileLogger }, null, null,
            Path.Join(Directory.GetCurrentDirectory(), $"log-{DateTime.Now:dd-M-yyyy-HH-mm-ss}.txt"));
        
        logger.WriteLine("Tranga_CLI", "Loading Settings.");
        TaskManager.SettingsData settings;
        string settingsPath = Path.Join(Directory.GetCurrentDirectory(), "data.json");
        if (File.Exists(settingsPath))
            settings = TaskManager.LoadData(Directory.GetCurrentDirectory());
        else
            settings = new TaskManager.SettingsData(Directory.GetCurrentDirectory(), null, new HashSet<TrangaTask>());

            
        logger.WriteLine("Tranga_CLI", "User Input");
        Console.WriteLine($"Output folder path [{settings.downloadLocation}]:");
        string? tmpPath = Console.ReadLine();
        while(tmpPath is null)
            tmpPath = Console.ReadLine();
        if(tmpPath.Length > 0)
            settings.downloadLocation = tmpPath;
        
        Console.WriteLine($"Komga BaseURL [{settings.komga?.baseUrl}]:");
        string? tmpUrl = Console.ReadLine();
        while (tmpUrl is null)
            tmpUrl = Console.ReadLine();
        if (tmpUrl.Length > 0)
        {
            Console.WriteLine("Username:");
            string? tmpUser = Console.ReadLine();
            while (tmpUser is null || tmpUser.Length < 1)
                tmpUser = Console.ReadLine();
            
            Console.WriteLine("Password:");
            string tmpPass = string.Empty;
            ConsoleKey key;
            do
            {
                var keyInfo = Console.ReadKey(intercept: true);
                key = keyInfo.Key;

                if (key == ConsoleKey.Backspace && tmpPass.Length > 0)
                {
                    Console.Write("\b \b");
                    tmpPass = tmpPass[0..^1];
                }
                else if (!char.IsControl(keyInfo.KeyChar))
                {
                    Console.Write("*");
                    tmpPass += keyInfo.KeyChar;
                }
            } while (key != ConsoleKey.Enter);

            settings.komga = new Komga(tmpUrl, tmpUser, tmpPass, logger);
        }
        
        logger.WriteLine("Tranga_CLI", "Loaded.");
        TaskMode(settings, logger);
    }

    private static void TaskMode(TaskManager.SettingsData settings, Logger logger)
    {
        TaskManager taskManager = new (settings, logger);
        ConsoleKey selection = PrintMenu(taskManager, settings.downloadLocation, logger);
        while (selection != ConsoleKey.Q)
        {
            switch (selection)
            {
                case ConsoleKey.L:
                    PrintTasks(taskManager.GetAllTasks(), logger);
                    Console.WriteLine("Press any key.");
                    Console.ReadKey();
                    break;
                case ConsoleKey.C:
                    CreateTask(taskManager, settings, logger);
                    Console.WriteLine("Press any key.");
                    Console.ReadKey();
                    break;
                case ConsoleKey.D:
                    RemoveTask (taskManager, logger);
                    Console.WriteLine("Press any key.");
                    Console.ReadKey();
                    break;
                case ConsoleKey.E:
                    ExecuteTaskNow(taskManager, logger);
                    Console.WriteLine("Press any key.");
                    Console.ReadKey();
                    break;
                case ConsoleKey.S:
                    SearchTasks(taskManager, logger);
                    Console.WriteLine("Press any key.");
                    Console.ReadKey();
                    break;
                case ConsoleKey.R:
                    PrintTasks(taskManager.GetAllTasks().Where(eTask => eTask.state == TrangaTask.ExecutionState.Running).ToArray(), logger);
                    Console.WriteLine("Press any key.");
                    Console.ReadKey();
                    break;
            }
            selection = PrintMenu(taskManager, settings.downloadLocation, logger);
        }

        logger.WriteLine("Tranga_CLI", "Exiting.");
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

    private static ConsoleKey PrintMenu(TaskManager taskManager, string folderPath, Logger logger)
    {
        int taskCount = taskManager.GetAllTasks().Length;
        int taskRunningCount = taskManager.GetAllTasks().Count(task => task.state == TrangaTask.ExecutionState.Running);
        int taskEnqueuedCount =
            taskManager.GetAllTasks().Count(task => task.state == TrangaTask.ExecutionState.Enqueued);
        Console.Clear();
        Console.WriteLine($"Download Folder: {folderPath}");
        Console.WriteLine($"Tasks (Running/Queue/Total)): {taskRunningCount}/{taskEnqueuedCount}/{taskCount}");
        Console.WriteLine();
        Console.WriteLine($"{"C: Create Task",-30}{"L: List tasks",-30}");
        Console.WriteLine($"{"D: Delete Task",-30}{"R: List Running Tasks", -30}");
        Console.WriteLine($"{"E: Execute Task now",-30}{"S: Search Tasks", -30}");
        Console.WriteLine();
        Console.WriteLine($"{"U: Update this Screen",-30}{"Q: Exit",-30}");
        ConsoleKey selection = Console.ReadKey().Key;
        logger.WriteLine("Tranga_CLI", $"Menu selection: {selection}");
        return selection;
    }

    private static void PrintTasks(TrangaTask[] tasks, Logger logger)
    {
        logger.WriteLine("Tranga_CLI", "Printing Tasks");
        int taskCount = tasks.Length;
        int taskRunningCount = tasks.Count(task => task.state == TrangaTask.ExecutionState.Running);
        int taskEnqueuedCount = tasks.Count(task => task.state == TrangaTask.ExecutionState.Enqueued);
        Console.Clear();
        int tIndex = 0;
        Console.WriteLine($"Tasks (Running/Queue/Total): {taskRunningCount}/{taskEnqueuedCount}/{taskCount}");
        string header =
            $"{"",-5}{"Task",-20} | {"Last Executed",-20} | {"Reoccurrence",-12} | {"State",-10} | {"Connector",-15} | Publication/Manga";
        Console.WriteLine(header);
        Console.WriteLine(new string('-', header.Length));
        foreach(TrangaTask trangaTask in tasks)
            Console.WriteLine($"{tIndex++:000}: {trangaTask}");
    }

    private static void CreateTask(TaskManager taskManager, TaskManager.SettingsData settings, Logger logger)
    {
        logger.WriteLine("Tranga_CLI", "Menu: Creating Task");
        TrangaTask.Task? tmpTask = SelectTaskType(logger);
        if (tmpTask is null)
            return;
        TrangaTask.Task task = (TrangaTask.Task)tmpTask!;
                    
        Connector? connector = null;
        if (task != TrangaTask.Task.UpdateKomgaLibrary)
        {
            connector = SelectConnector(settings.downloadLocation, taskManager.GetAvailableConnectors().Values.ToArray(), logger);
            if (connector is null)
                return;
        }
                    
        Publication? publication = null;
        if (task != TrangaTask.Task.UpdatePublications && task != TrangaTask.Task.UpdateKomgaLibrary)
        {
            publication = SelectPublication(connector!, logger);
            if (publication is null)
                return;
        }
        
        TimeSpan reoccurrence = SelectReoccurrence(logger);
        logger.WriteLine("Tranga_CLI", "Sending Task to TaskManager");
        TrangaTask newTask = taskManager.AddTask(task, connector?.name, publication, reoccurrence, "en");
        Console.WriteLine(newTask);
    }

    private static void ExecuteTaskNow(TaskManager taskManager, Logger logger)
    {
        logger.WriteLine("Tranga_CLI", "Menu: Executing Task");
        TrangaTask[] tasks = taskManager.GetAllTasks();
        if (tasks.Length < 1)
        {
            Console.Clear();
            Console.WriteLine("There are no available Tasks.");
            logger.WriteLine("Tranga_CLI", "No available Tasks.");
            return;
        }
        PrintTasks(tasks, logger);
        
        logger.WriteLine("Tranga_CLI", "Selecting Task to Execute");
        Console.WriteLine("Enter q to abort");
        Console.WriteLine($"Select Task (0-{tasks.Length - 1}):");

        string? selectedTask = Console.ReadLine();
        while(selectedTask is null || selectedTask.Length < 1)
            selectedTask = Console.ReadLine();
        
        if (selectedTask.Length == 1 && selectedTask.ToLower() == "q")
        {
            Console.Clear();
            Console.WriteLine("aborted.");
            logger.WriteLine("Tranga_CLI", "aborted");
            return;
        }
        
        try
        {
            int selectedTaskIndex = Convert.ToInt32(selectedTask);
            logger.WriteLine("Tranga_CLI", "Sending Task to TaskManager");
            taskManager.ExecuteTaskNow(tasks[selectedTaskIndex]);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Exception: {e.Message}");
            logger.WriteLine("Tranga_CLI", e.Message);
        }
        
    }

    private static void RemoveTask(TaskManager taskManager, Logger logger)
    {
        logger.WriteLine("Tranga_CLI", "Menu: Remove Task");
        TrangaTask[] tasks = taskManager.GetAllTasks();
        if (tasks.Length < 1)
        {
            Console.Clear();
            Console.WriteLine("There are no available Tasks.");
            logger.WriteLine("Tranga_CLI", "No available Tasks");
            return;
        }
        PrintTasks(tasks, logger);
        
        logger.WriteLine("Tranga_CLI", "Selecting Task");
        Console.WriteLine("Enter q to abort");
        Console.WriteLine($"Select Task (0-{tasks.Length - 1}):");

        string? selectedTask = Console.ReadLine();
        while(selectedTask is null || selectedTask.Length < 1)
            selectedTask = Console.ReadLine();

        if (selectedTask.Length == 1 && selectedTask.ToLower() == "q")
        {
            Console.Clear();
            Console.WriteLine("aborted.");
            logger.WriteLine("Tranga_CLI", "aborted.");
            return;
        }
        
        try
        {
            int selectedTaskIndex = Convert.ToInt32(selectedTask);
            logger.WriteLine("Tranga_CLI", "Sending Task to TaskManager");
            taskManager.RemoveTask(tasks[selectedTaskIndex].task, tasks[selectedTaskIndex].connectorName, tasks[selectedTaskIndex].publication);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Exception: {e.Message}");
            logger.WriteLine("Tranga_CLI", e.Message);
        }
    }

    private static TrangaTask.Task? SelectTaskType(Logger logger)
    {
        logger.WriteLine("Tranga_CLI", "Menu: Select TaskType");
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
            logger.WriteLine("Tranga_CLI", "aborted.");
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
            logger.WriteLine("Tranga_CLI", e.Message);
        }

        return null;
    }

    private static TimeSpan SelectReoccurrence(Logger logger)
    {
        logger.WriteLine("Tranga_CLI", "Menu: Select Reoccurrence");
        Console.WriteLine("Select reoccurrence Timer (Format hh:mm:ss):");
        return TimeSpan.Parse(Console.ReadLine()!, new CultureInfo("en-US"));
    }

    private static Connector? SelectConnector(string folderPath, Connector[] connectors, Logger logger)
    {
        logger.WriteLine("Tranga_CLI", "Menu: Select Connector");
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
            logger.WriteLine("Tranga_CLI", "aborted.");
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
            logger.WriteLine("Tranga_CLI", e.Message);
        }

        return null;
    }

    private static Publication? SelectPublication(Connector connector, Logger logger)
    {
        logger.WriteLine("Tranga_CLI", "Menu: Select Publication");
        
        Console.Clear();
        Console.WriteLine($"Connector: {connector.name}");
        Console.WriteLine("Publication search query (leave empty for all):");
        string? query = Console.ReadLine();

        Publication[] publications = connector.GetPublications(query ?? "");
        
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
            logger.WriteLine("Tranga_CLI", "aborted.");
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
            logger.WriteLine("Tranga_CLI", e.Message);
        }

        return null;
    }

    private static void SearchTasks(TaskManager taskManager, Logger logger)
    {
        logger.WriteLine("Tranga_CLI", "Menu: Search task");
        Console.Clear();
        Console.WriteLine("Enter search query:");
        string? query = Console.ReadLine();
        while (query is null || query.Length < 4)
            query = Console.ReadLine();
        PrintTasks(taskManager.GetAllTasks().Where(qTask =>
            qTask.ToString().ToLower().Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray(), logger);
    }
}