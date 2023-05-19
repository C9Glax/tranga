using System.Globalization;
using Tranga;
using Tranga.Connectors;

namespace Tranga_CLI;

public static class Tranga_Cli
{
    public static void Main(string[] args)
    {
        Console.WriteLine("Output folder path [standard D:]:");
        string? folderPath = Console.ReadLine();
        while(folderPath is null )
            folderPath = Console.ReadLine();
        if (folderPath.Length < 1)
            folderPath = "D:";
        
        Console.Write("Mode (D: Interactive only, T: TaskManager):");
        ConsoleKeyInfo mode = Console.ReadKey();
        while (mode.Key != ConsoleKey.D && mode.Key != ConsoleKey.T)
            mode = Console.ReadKey();
        Console.WriteLine();
        
        if(mode.Key == ConsoleKey.D)
            DownloadNow(folderPath);
        else if (mode.Key == ConsoleKey.T)
            TaskMode(folderPath);
    }

    private static void TaskMode(string folderPath)
    {
        TaskManager taskManager = new TaskManager(folderPath);
        ConsoleKey selection = ConsoleKey.NoName;
        int menu = 0;
        while (selection != ConsoleKey.Escape && selection != ConsoleKey.Q)
        {
            switch (menu)
            {
                case 1:
                    PrintTasks(taskManager);
                    Console.WriteLine("Press any key.");
                    Console.ReadKey();
                    menu = 0;
                    break;
                case 2:
                    Connector connector = SelectConnector(folderPath, taskManager.GetAvailableConnectors().Values.ToArray());
                    TrangaTask.Task task = SelectTask();
                    Publication? publication = null;
                    if(task != TrangaTask.Task.UpdatePublications)
                        publication = SelectPublication(connector);
                    TimeSpan reoccurrence = SelectReoccurrence();
                    taskManager.AddTask(task, connector.name, publication, reoccurrence, "en");
                    Console.WriteLine($"{task} - {connector.name} - {publication?.sortName}");
                    Console.WriteLine("Press any key.");
                    Console.ReadKey();
                    menu = 0;
                    break;
                case 3:
                    RemoveTask(taskManager);
                    Console.WriteLine("Press any key.");
                    Console.ReadKey();
                    menu = 0;
                    break;
                case 4:
                    ExecuteTask(taskManager);
                    Console.WriteLine("Press any key.");
                    Console.ReadKey();
                    menu = 0;
                    break;
                default:
                    selection = Menu(folderPath);
                    switch (selection)
                    {
                        case ConsoleKey.L:
                            menu = 1;
                            break;
                        case ConsoleKey.C:
                            menu = 2;
                            break;
                        case ConsoleKey.D:
                            menu = 3;
                            break;
                        case ConsoleKey.E:
                            menu = 4;
                            break;
                        default:
                            menu = 0;
                            break;
                    }
                    break;
            }
        }
        taskManager.Shutdown();
    }

    private static ConsoleKey Menu(string folderPath)
    {
        Console.Clear();
        Console.WriteLine($"Download Folder: {folderPath}");
        Console.WriteLine("Select Option:");
        Console.WriteLine("L: List tasks");
        Console.WriteLine("C: Create Task");
        Console.WriteLine("D: Delete Task");
        Console.WriteLine("E: Execute Task now");
        Console.WriteLine("Q: Exit with saving");
        ConsoleKey selection = Console.ReadKey().Key;
        Console.WriteLine();
        return selection;
    }

    private static int PrintTasks(TaskManager taskManager)
    {
        Console.Clear();
        TrangaTask[] tasks = taskManager.GetAllTasks();
        int tIndex = 0;
        Console.WriteLine("Tasks:");
        foreach(TrangaTask trangaTask in tasks)
            Console.WriteLine($"{tIndex++}: {trangaTask.task} - {trangaTask.reoccurrence} - {trangaTask.publication?.sortName} - {trangaTask.connectorName} - {trangaTask.lastExecuted}");
        return tasks.Length;
    }

    private static void RemoveTask(TaskManager taskManager)
    {
        int length = PrintTasks(taskManager);
        
        TrangaTask[] tasks = taskManager.GetAllTasks();
        Console.WriteLine($"Select Task (0-{length}):");

        string? selectedTask = Console.ReadLine();
        while(selectedTask is null || selectedTask.Length < 1)
            selectedTask = Console.ReadLine();
        int selectedTaskIndex = Convert.ToInt32(selectedTask);

        taskManager.RemoveTask(tasks[selectedTaskIndex].task, tasks[selectedTaskIndex].connectorName, tasks[selectedTaskIndex].publication);
    }

    private static TrangaTask.Task SelectTask()
    {
        Console.Clear();
        string[] taskNames = Enum.GetNames<TrangaTask.Task>();
        
        int tIndex = 0;
        Console.WriteLine("Available Tasks:");
        foreach (string taskName in taskNames)
            Console.WriteLine($"{tIndex++}: {taskName}");
        Console.WriteLine($"Select Task (0-{taskNames.Length - 1}):");

        string? selectedTask = Console.ReadLine();
        while(selectedTask is null || selectedTask.Length < 1)
            selectedTask = Console.ReadLine();
        int selectedTaskIndex = Convert.ToInt32(selectedTask);

        string selectedTaskName = taskNames[selectedTaskIndex];
        return Enum.Parse<TrangaTask.Task>(selectedTaskName);
    }

    private static TimeSpan SelectReoccurrence()
    {
        Console.WriteLine("Select reoccurrence Timer (Format hh:mm:ss):");
        return TimeSpan.Parse(Console.ReadLine()!, new CultureInfo("en-US"));
    }

    private static void DownloadNow(string folderPath)
    {
        Connector connector = SelectConnector(folderPath);

        Publication publication = SelectPublication(connector);
        
        Chapter[] downloadChapters = SelectChapters(connector, publication);

        if (downloadChapters.Length > 0)
        {
            connector.DownloadCover(publication);
            connector.SaveSeriesInfo(publication);
        }

        foreach (Chapter chapter in downloadChapters)
        {
            Console.WriteLine($"Downloading {publication.sortName} V{chapter.volumeNumber}C{chapter.chapterNumber}");
            connector.DownloadChapter(publication, chapter);
        }
    }

    private static Connector SelectConnector(string folderPath, Connector[]? availableConnectors = null)
    {
        Console.Clear();
        Connector[] connectors = availableConnectors ?? new Connector[] { new MangaDex(folderPath) };
        
        int cIndex = 0;
        Console.WriteLine("Connectors:");
        foreach (Connector connector in connectors)
            Console.WriteLine($"{cIndex++}: {connector.name}");
        Console.WriteLine($"Select Connector (0-{connectors.Length - 1}):");

        string? selectedConnector = Console.ReadLine();
        while(selectedConnector is null || selectedConnector.Length < 1)
            selectedConnector = Console.ReadLine();
        int selectedConnectorIndex = Convert.ToInt32(selectedConnector);
        
        return connectors[selectedConnectorIndex];
    }

    private static Publication SelectPublication(Connector connector)
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
        Console.WriteLine($"Select publication to Download (0-{publications.Length - 1}):");
        
        string? selected = Console.ReadLine();
        while(selected is null || selected.Length < 1)
            selected = Console.ReadLine();
        return publications[Convert.ToInt32(selected)];
    }

    private static Chapter[] SelectChapters(Connector connector, Publication publication)
    {
        Console.Clear();
        Console.WriteLine($"Connector: {connector.name} Publication: {publication.sortName}");
        Chapter[] chapters = connector.GetChapters(publication, "en");
        
        int cIndex = 0;
        Console.WriteLine("Chapters:");
        foreach (Chapter ch in chapters)
        {
            string name = cIndex.ToString();
            if (ch.name is not null && ch.name.Length > 0)
                name = ch.name;
            else if (ch.chapterNumber is not null && ch.chapterNumber.Length > 0)
                name = ch.chapterNumber;
            Console.WriteLine($"{cIndex++}: {name}");
        }
        Console.WriteLine($"Select Chapters to download (0-{chapters.Length - 1}) [range x-y or 'a' for all]: ");
        string? selected = Console.ReadLine();
        while(selected is null || selected.Length < 1)
            selected = Console.ReadLine();

        int start = 0;
        int end = 0;
        if (selected == "a")
            end = chapters.Length - 1;
        else if (selected.Contains('-'))
        {
            string[] split = selected.Split('-');
            start = Convert.ToInt32(split[0]);
            end = Convert.ToInt32(split[1]);
        }
        else
        {
            start = Convert.ToInt32(selected);
            end = Convert.ToInt32(selected);
        }
        
        return chapters.Skip(start).Take((end + 1)-start).ToArray();
    }
}