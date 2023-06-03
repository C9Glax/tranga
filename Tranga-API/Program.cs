using System.Runtime.InteropServices;
using Logging;
using Tranga;

string applicationFolderPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Tranga-API");
string downloadFolderPath = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/Manga" : Path.Join(applicationFolderPath, "Manga");
string logsFolderPath = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "/var/logs/Tranga" : Path.Join(applicationFolderPath, "logs");
string logFilePath = Path.Join(logsFolderPath, $"log-{DateTime.Now:dd-M-yyyy-HH-mm-ss}.txt");
string settingsFilePath = Path.Join(applicationFolderPath, "settings.json");

Directory.CreateDirectory(logsFolderPath);
Logger logger = new(new[] { Logger.LoggerType.FileLogger, Logger.LoggerType.ConsoleLogger }, Console.Out, Console.Out.Encoding, logFilePath);

logger.WriteLine("Tranga", "Loading settings.");

TrangaSettings settings;
if (File.Exists(settingsFilePath))
    settings = TrangaSettings.LoadSettings(settingsFilePath, logger);
else
    settings = new TrangaSettings(downloadFolderPath, applicationFolderPath, new HashSet<LibraryManager>());

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

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers().AddNewtonsoftJson();

string corsHeader = "Tranga";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: corsHeader,
        policy  =>
        {
            policy.AllowAnyOrigin();
            policy.WithMethods("GET", "POST", "DELETE");
        });
});

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors(corsHeader);

app.MapGet("/Tranga/GetAvailableControllers", () =>  taskManager.GetAvailableConnectors().Keys.ToArray());

app.MapGet("/Tranga/GetKnownPublications", () => taskManager.GetAllPublications());

app.MapGet("/Tranga/GetPublicationsFromConnector", (string connectorName, string title) =>
{
    Connector? connector = taskManager.GetAvailableConnectors().FirstOrDefault(con => con.Key == connectorName).Value;
    if (connector is null)
        return Array.Empty<Publication>();
    if(title.Length < 4)
        return Array.Empty<Publication>();
    return taskManager.GetPublicationsFromConnector(connector, title);
});

app.MapGet("/Tasks/GetTaskTypes", () => Enum.GetNames(typeof(TrangaTask.Task)));


app.MapPost("/Tasks/Create", (string taskType, string? connectorName, string? publicationId, string reoccurrenceTime, string? language) =>
{
    Publication? publication = taskManager.GetAllPublications().FirstOrDefault(pub => pub.internalId == publicationId);
    TrangaTask.Task task = Enum.Parse<TrangaTask.Task>(taskType);
    taskManager.AddTask(task, connectorName, publication, TimeSpan.Parse(reoccurrenceTime), language??"");
});

app.MapDelete("/Tasks/Delete", (string taskType, string? connectorName, string? publicationId) =>
{
    Publication? publication = taskManager.GetAllPublications().FirstOrDefault(pub => pub.internalId == publicationId);
    TrangaTask.Task task = Enum.Parse<TrangaTask.Task>(taskType);
    taskManager.DeleteTask(task, connectorName, publication);
});

app.MapGet("/Tasks/Get", (string taskType, string? connectorName, string? searchString) =>
{
    try
    {
        TrangaTask.Task task = Enum.Parse<TrangaTask.Task>(taskType);
        if (searchString is null || connectorName is null)
            return taskManager.GetAllTasks().Where(tTask => tTask.task == task);
        else
            return taskManager.GetAllTasks().Where(tTask =>
                tTask.task == task && tTask.connectorName == connectorName && tTask.ToString()
                    .Contains(searchString, StringComparison.InvariantCultureIgnoreCase));
    }
    catch (ArgumentException)
    {
        return Array.Empty<TrangaTask>();
    }
});

app.MapGet("/Tasks/GetTaskProgress", (string taskType, string? connectorName, string? publicationId) =>
{
    try
    {
        TrangaTask.Task pTask = Enum.Parse<TrangaTask.Task>(taskType);
        TrangaTask? task = null;
        if (connectorName is null || publicationId is null)
            task = taskManager.GetAllTasks().FirstOrDefault(tTask =>
                tTask.task == pTask);
        else
            task = taskManager.GetAllTasks().FirstOrDefault(tTask =>
                tTask.task == pTask && tTask.publication?.internalId == publicationId &&
                tTask.connectorName == connectorName);
            
        if (task is null)
            return -1f;

        return task.progress;
    }
    catch (ArgumentException)
    {
        return -1f;
    }
});

app.MapPost("/Tasks/Start", (string taskType, string? connectorName, string? publicationId) =>
{
    try
    {
        TrangaTask.Task pTask = Enum.Parse<TrangaTask.Task>(taskType);
        TrangaTask? task = null;
        if (connectorName is null || publicationId is null)
            task = taskManager.GetAllTasks().FirstOrDefault(tTask =>
                tTask.task == pTask);
        else
            task = taskManager.GetAllTasks().FirstOrDefault(tTask =>
                tTask.task == pTask && tTask.publication?.internalId == publicationId &&
                tTask.connectorName == connectorName);
            
        if (task is null)
            return;
        taskManager.ExecuteTaskNow(task);
    }
    catch (ArgumentException)
    {
        return;
    }

});

app.MapGet("/Tasks/GetRunningTasks",
    () => taskManager.GetAllTasks().Where(task => task.state is TrangaTask.ExecutionState.Running));

app.MapGet("/Queue/GetList",
    () => taskManager.GetAllTasks().Where(task => task.state is TrangaTask.ExecutionState.Enqueued));

app.MapPost("/Queue/Enqueue", (string taskType, string? connectorName, string? publicationId) =>
{
    try
    {
        TrangaTask.Task pTask = Enum.Parse<TrangaTask.Task>(taskType);
        TrangaTask? task = null;
        if (connectorName is null || publicationId is null)
            task = taskManager.GetAllTasks().FirstOrDefault(tTask =>
                tTask.task == pTask);
        else
            task = taskManager.GetAllTasks().FirstOrDefault(tTask =>
                tTask.task == pTask && tTask.publication?.internalId == publicationId &&
                tTask.connectorName == connectorName);
            
        if (task is null)
            return;
        taskManager.AddTaskToQueue(task);
    }
    catch (ArgumentException)
    {
        return;
    }
});

app.MapDelete("/Queue/Dequeue", (string taskType, string? connectorName, string? publicationId) =>
{
    try
    {
        TrangaTask.Task pTask = Enum.Parse<TrangaTask.Task>(taskType);
        TrangaTask? task = null;
        if (connectorName is null || publicationId is null)
            task = taskManager.GetAllTasks().FirstOrDefault(tTask =>
                tTask.task == pTask);
        else
            task = taskManager.GetAllTasks().FirstOrDefault(tTask =>
                tTask.task == pTask && tTask.publication?.internalId == publicationId &&
                tTask.connectorName == connectorName);
            
        if (task is null)
            return;
        taskManager.RemoveTaskFromQueue(task);
    }
    catch (ArgumentException)
    {
        return;
    }
});

app.MapGet("/Settings/Get", () => taskManager.settings);

app.MapPost("/Settings/Update",
    (string? downloadLocation, string? komgaUrl, string? komgaAuth, string? kavitaUrl, string? kavitaUsername, string? kavitaPassword) =>
        taskManager.UpdateSettings(downloadLocation, komgaUrl, komgaAuth, kavitaUrl, kavitaUsername, kavitaPassword));

app.Run();