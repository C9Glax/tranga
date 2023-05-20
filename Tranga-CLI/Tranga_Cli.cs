using System.Globalization;
using Tranga;
using Tranga.Connectors;

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
        TaskManager.SettingsData settings;
        string settingsPath = Path.Join(Directory.GetCurrentDirectory(), "data.json");
        if (File.Exists(settingsPath))
            settings = TaskManager.LoadData(Directory.GetCurrentDirectory());
        else
            settings = new TaskManager.SettingsData(Directory.GetCurrentDirectory(), null, new HashSet<TrangaTask>());

            
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

            settings.komga = new Komga(tmpUrl, tmpUser, tmpPass);
        }
        
        TaskMode(settings);
    }

    private static void TaskMode(TaskManager.SettingsData settings)
    {
        TaskManager taskManager = new (settings);
        ConsoleKey selection = PrintMenu(taskManager, settings.downloadLocation);
        while (selection != ConsoleKey.Q)
        {
            switch (selection)
            {
                case ConsoleKey.L:
                    PrintTasks(taskManager.GetAllTasks());
                    Console.WriteLine("Press any key.");
                    Console.ReadKey();
                    break;
                case ConsoleKey.C:
                    CreateTask(taskManager, settings);
                    Console.WriteLine("Press any key.");
                    Console.ReadKey();
                    break;
                case ConsoleKey.D:
                    RemoveTask (taskManager);
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
                    PrintTasks(taskManager.GetAllTasks().Where(eTask => eTask.state == TrangaTask.ExecutionState.Running).ToArray());
                    Console.WriteLine("Press any key.");
                    Console.ReadKey();
                    break;
            }
            selection = PrintMenu(taskManager, settings.downloadLocation);
        }

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

    private static ConsoleKey PrintMenu(TaskManager taskManager, string folderPath)
    {
        int taskCount = taskManager.GetAllTasks().Length;
        int taskRunningCount = taskManager.GetAllTasks().Count(task => task.state == TrangaTask.ExecutionState.Running);
        int taskEnqueuedCount =
            taskManager.GetAllTasks().Count(task => task.state == TrangaTask.ExecutionState.Enqueued);
        Console.Clear();
        Console.WriteLine($"Download Folder: {folderPath} Tasks (Running/Queue/Total): {taskRunningCount}/{taskEnqueuedCount}/{taskCount}");
        Console.WriteLine("U: Update this Screen");
        Console.WriteLine("L: List tasks");
        Console.WriteLine("C: Create Task");
        Console.WriteLine("D: Delete Task");
        Console.WriteLine("E: Execute Task now");
        Console.WriteLine("S: Search Task");
        Console.WriteLine("R: Running Tasks");
        Console.WriteLine("Q: Exit");
        ConsoleKey selection = Console.ReadKey().Key;
        Console.WriteLine();
        return selection;
    }

    private static void PrintTasks(TrangaTask[] tasks)
    {
        int taskCount = tasks.Length;
        int taskRunningCount = tasks.Count(task => task.state == TrangaTask.ExecutionState.Running);
        int taskEnqueuedCount = tasks.Count(task => task.state == TrangaTask.ExecutionState.Enqueued);
        Console.Clear();
        int tIndex = 0;
        Console.WriteLine($"Tasks (Running/Queue/Total): {taskRunningCount}/{taskEnqueuedCount}/{taskCount}");
        foreach(TrangaTask trangaTask in tasks)
            Console.WriteLine($"{tIndex++:000}: {trangaTask}");
    }

    private static void CreateTask(TaskManager taskManager, TaskManager.SettingsData settings)
    {
        TrangaTask.Task? tmpTask = SelectTaskType();
        if (tmpTask is null)
            return;
        TrangaTask.Task task = (TrangaTask.Task)tmpTask!;
                    
        Connector? connector = null;
        if (task != TrangaTask.Task.UpdateKomgaLibrary)
        {
            connector = SelectConnector(settings.downloadLocation, taskManager.GetAvailableConnectors().Values.ToArray());
            if (connector is null)
                return;
        }
                    
        Publication? publication = null;
        if (task != TrangaTask.Task.UpdatePublications && task != TrangaTask.Task.UpdateKomgaLibrary)
        {
            publication = SelectPublication(connector!);
            if (publication is null)
                return;
        }
                    
        TimeSpan reoccurrence = SelectReoccurrence();
        TrangaTask newTask = taskManager.AddTask(task, connector?.name, publication, reoccurrence, "en");
        Console.WriteLine(newTask);
    }

    private static void ExecuteTaskNow(TaskManager taskManager)
    {
        TrangaTask[] tasks = taskManager.GetAllTasks();
        if (tasks.Length < 1)
        {
            Console.Clear();
            Console.WriteLine("There are no available Tasks.");
            return;
        }
        PrintTasks(tasks);
        
        Console.WriteLine("Enter q to abort");
        Console.WriteLine($"Select Task (0-{tasks.Length - 1}):");

        string? selectedTask = Console.ReadLine();
        while(selectedTask is null || selectedTask.Length < 1)
            selectedTask = Console.ReadLine();
        
        if (selectedTask.Length == 1 && selectedTask.ToLower() == "q")
        {
            Console.Clear();
            Console.WriteLine("aborted.");
            return;
        }
        
        try
        {
            int selectedTaskIndex = Convert.ToInt32(selectedTask);
            taskManager.ExecuteTaskNow(tasks[selectedTaskIndex]);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Exception: {e.Message}");
        }
        
    }

    private static void RemoveTask(TaskManager taskManager)
    {
        TrangaTask[] tasks = taskManager.GetAllTasks();
        if (tasks.Length < 1)
        {
            Console.Clear();
            Console.WriteLine("There are no available Tasks.");
            return;
        }
        PrintTasks(tasks);
        
        Console.WriteLine("Enter q to abort");
        Console.WriteLine($"Select Task (0-{tasks.Length - 1}):");

        string? selectedTask = Console.ReadLine();
        while(selectedTask is null || selectedTask.Length < 1)
            selectedTask = Console.ReadLine();

        if (selectedTask.Length == 1 && selectedTask.ToLower() == "q")
        {
            Console.Clear();
            Console.WriteLine("aborted.");
            return;
        }
        
        try
        {
            int selectedTaskIndex = Convert.ToInt32(selectedTask);
            taskManager.RemoveTask(tasks[selectedTaskIndex].task, tasks[selectedTaskIndex].connectorName, tasks[selectedTaskIndex].publication);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Exception: {e.Message}");
        }
    }

    private static TrangaTask.Task? SelectTaskType()
    {
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
        }

        return null;
    }

    private static TimeSpan SelectReoccurrence()
    {
        Console.WriteLine("Select reoccurrence Timer (Format hh:mm:ss):");
        return TimeSpan.Parse(Console.ReadLine()!, new CultureInfo("en-US"));
    }

    private static Connector? SelectConnector(string folderPath, Connector[] connectors)
    {
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
        }

        return null;
    }

    private static Publication? SelectPublication(Connector connector)
    {
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
        }

        return null;
    }

    private static void SearchTasks(TaskManager taskManager)
    {
        string? query = Console.ReadLine();
        while (query is null || query.Length < 1)
            query = Console.ReadLine();
        PrintTasks(taskManager.GetAllTasks().Where(qTask =>
            qTask.ToString().ToLower().Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray());
    }
}