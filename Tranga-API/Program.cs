using System.Text.Json;
using Tranga;
using Logging;

string applicationFolderPath =  Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Tranga-API");
string logsFolderPath = Path.Join(applicationFolderPath, "logs");
string logFilePath = Path.Join(logsFolderPath, $"log-{DateTime.Now:dd-M-yyyy-HH-mm-ss}.txt");
string settingsFilePath = Path.Join(applicationFolderPath, "data.json");

Console.WriteLine($"Logfile-Path: {logFilePath}");
Console.WriteLine($"Settings-File-Path: {settingsFilePath}");

Logger logger = new(new[] { Logger.LoggerType.FileLogger }, null, null, logFilePath);

logger.WriteLine("Tranga_API", "Loading Taskmanager.");

TaskManager.SettingsData settings;
if (File.Exists(settingsFilePath))
    settings = TaskManager.LoadData(settingsFilePath);
else
    settings = new TaskManager.SettingsData(Directory.GetCurrentDirectory(), settingsFilePath, null, new HashSet<TrangaTask>());

TaskManager taskManager = new (settings, logger);

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/GetConnectors", () => JsonSerializer.Serialize(taskManager.GetAvailableConnectors().Values.ToArray()));

app.MapGet("/GetPublications", (string connectorName, string? publicationName) =>
{
    Connector connector = taskManager.GetConnector(connectorName);

    Publication[] publications;
    if (publicationName is not null)
        publications = connector.GetPublications(publicationName);
    else
        publications = connector.GetPublications();

    return JsonSerializer.Serialize(publications);
});

app.MapGet("/ListTasks", () => JsonSerializer.Serialize(taskManager.GetAllTasks()));

app.MapGet("/TaskTypes", () =>
{
    string[] availableTasks = Enum.GetNames(typeof(TrangaTask.Task));
    return JsonSerializer.Serialize(availableTasks);
});

app.MapGet("/CreateTask",
    (TrangaTask.Task task, string? connectorName, string? publicationInternalId, TimeSpan reoccurrence, string? language) =>
    {
        switch (task)
        {
            case TrangaTask.Task.UpdateKomgaLibrary:
                taskManager.AddTask(TrangaTask.Task.UpdateKomgaLibrary, null, null, reoccurrence);
                break;
            case TrangaTask.Task.DownloadNewChapters:
                try
                {
                    Publication? publication = taskManager.GetAllPublications()
                        .FirstOrDefault(pub => pub.internalId == publicationInternalId);

                    if (publication is null)
                    {
                        return JsonSerializer.Serialize($"Publication {publicationInternalId} is unknown.");
                    }
                    else
                    {
                        taskManager.AddTask(TrangaTask.Task.DownloadNewChapters, connectorName, publication, reoccurrence, language ?? "");
                        return JsonSerializer.Serialize("Success");
                    }
                }
                catch (Exception e)
                {
                    return JsonSerializer.Serialize(e.Message);
                }

            default: return JsonSerializer.Serialize("Not Implemented");
        }

        return JsonSerializer.Serialize("Not Implemented");
    });

app.MapGet("/RemoveTask", (TrangaTask.Task task, string? connectorName, string? publicationInternalId) =>
{
    switch (task)
    {
        case TrangaTask.Task.UpdateKomgaLibrary:
            taskManager.DeleteTask(TrangaTask.Task.UpdateKomgaLibrary, null, null);
            return JsonSerializer.Serialize("Success");
        case TrangaTask.Task.DownloadNewChapters:
            Publication? publication = taskManager.GetAllPublications().FirstOrDefault(pub => pub.internalId == publicationInternalId);
            if (publication is null)
                JsonSerializer.Serialize($"Publication with id {publicationInternalId} is unknown.");
            
            taskManager.DeleteTask(TrangaTask.Task.DownloadNewChapters, connectorName, publication);
            
            return JsonSerializer.Serialize("Success");
        
        default: return JsonSerializer.Serialize("Not Implemented");
    }
});

app.Run();