
using Logging;
using Tranga;

string applicationFolderPath =  Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Tranga-API");
string logsFolderPath = Path.Join(applicationFolderPath, "logs");
string logFilePath = Path.Join(logsFolderPath, $"log-{DateTime.Now:dd-M-yyyy-HH-mm-ss}.txt");
string settingsFilePath = Path.Join(applicationFolderPath, "data.json");

Directory.CreateDirectory(applicationFolderPath);
Directory.CreateDirectory(logsFolderPath);
        
Console.WriteLine($"Logfile-Path: {logFilePath}");
Console.WriteLine($"Settings-File-Path: {settingsFilePath}");

Logger logger = new(new[] { Logger.LoggerType.FileLogger }, null, null, logFilePath);
        
logger.WriteLine("Tranga_CLI", "Loading Taskmanager.");
TaskManager.SettingsData settings;
if (File.Exists(settingsFilePath))
    settings = TaskManager.LoadData(settingsFilePath);
else
    settings = new TaskManager.SettingsData(Directory.GetCurrentDirectory(), settingsFilePath, null, new HashSet<TrangaTask>());

TaskManager taskManager = new (settings, logger);

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers().AddNewtonsoftJson();
var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
app.UseSwagger();
app.UseSwaggerUI();

app.MapGet("/GetAvailableControllers", () =>  taskManager.GetAvailableConnectors());

app.MapGet("/GetKnownPublications", () => taskManager.GetAllPublications());

app.MapGet("/GetPublicationsFromConnector", (string connectorName, string title) =>
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

app.MapPost("/Tasks/Delete", (string taskType, string? connectorName, string? publicationId) =>
{
    Publication? publication = taskManager.GetAllPublications().FirstOrDefault(pub => pub.internalId == publicationId);
    TrangaTask.Task task = Enum.Parse<TrangaTask.Task>(taskType);
    taskManager.DeleteTask(task, connectorName, publication);
});

app.MapGet("/Tasks/GetList", () => taskManager.GetAllTasks());

app.MapPost("/Tasks/Start", (string taskType, string? connectorName, string? publicationId) =>
{
    TrangaTask.Task pTask = Enum.Parse<TrangaTask.Task>(taskType);
    TrangaTask? task = taskManager.GetAllTasks().FirstOrDefault(tTask =>
        tTask.task == pTask && tTask.publication?.internalId == publicationId && tTask.connectorName == connectorName);
    if (task is null)
        return;
    taskManager.ExecuteTaskNow(task);
});

app.MapGet("/Tasks/GetRunningTasks",
    () => taskManager.GetAllTasks().Where(task => task.state is TrangaTask.ExecutionState.Running));

app.MapGet("/Queue/GetList",
    () => taskManager.GetAllTasks().Where(task => task.state is TrangaTask.ExecutionState.Enqueued));

app.MapPost("/Queue/Enqueue", (string taskType, string? connectorName, string? publicationId) =>
{
    TrangaTask.Task pTask = Enum.Parse<TrangaTask.Task>(taskType);
    TrangaTask? task = taskManager.GetAllTasks().FirstOrDefault(tTask =>
        tTask.task == pTask && tTask.publication?.internalId == publicationId && tTask.connectorName == connectorName);
    if (task is null)
        return;
    taskManager.AddTaskToQueue(task);
});

app.MapPost("/Queue/Dequeue", (string taskType, string? connectorName, string? publicationId) =>
{
    TrangaTask.Task pTask = Enum.Parse<TrangaTask.Task>(taskType);
    TrangaTask? task = taskManager.GetAllTasks().FirstOrDefault(tTask =>
        tTask.task == pTask && tTask.publication?.internalId == publicationId && tTask.connectorName == connectorName);
    if (task is null)
        return;
    taskManager.RemoveTaskFromQueue(task);
});

app.MapGet("/Settings/Get", () => new Settings(taskManager.settings));

app.MapPost("/Settings/Update", (string? downloadLocation, string? komgaUrl, string? komgaAuth) =>
{
    if(downloadLocation is not null)
        taskManager.settings.downloadLocation = downloadLocation;
    if(komgaUrl is not null && komgaAuth is not null)
        taskManager.settings.komga = new Komga(komgaUrl, komgaAuth, logger);
});

app.Run();

class Settings
{
    public string downloadLocation { get; }
    public Komga? komga { get; }

    public Settings(TaskManager.SettingsData settings)
    {
        this.downloadLocation = settings.downloadLocation;
        this.komga = settings.komga;
    }
}