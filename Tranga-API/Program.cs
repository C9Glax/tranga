using Logging;
using Tranga;

string applicationFolderPath =
    Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Tranga-API");
string downloadFolderPath = Path.Join(applicationFolderPath, "Manga");
string logsFolderPath = Path.Join(applicationFolderPath, "logs");
string logFilePath = Path.Join(logsFolderPath, $"log-{DateTime.Now:dd-M-yyyy-HH-mm-ss}.txt");
string settingsFilePath = Path.Join(applicationFolderPath, "settings.json");

Directory.CreateDirectory(applicationFolderPath);
Directory.CreateDirectory(logsFolderPath);
        
Console.WriteLine($"Logfile-Path: {logFilePath}");
Console.WriteLine($"Settings-File-Path: {settingsFilePath}");

Logger logger = new(new[] { Logger.LoggerType.FileLogger }, null, null, logFilePath);
        
logger.WriteLine("Tranga_CLI", "Loading Taskmanager.");
TrangaSettings settings;
if (File.Exists(settingsFilePath))
    settings = TrangaSettings.LoadSettings(settingsFilePath);
else
    settings = new TrangaSettings(downloadFolderPath, applicationFolderPath, null);

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
            policy.WithOrigins("http://localhost", "http://127.0.0.1", "http://localhost:63342");
            policy.WithMethods("GET", "POST", "DELETE");
        });
});

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();
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
    TrangaTask.Task task = Enum.Parse<TrangaTask.Task>(taskType);
    if (searchString is null)
        return taskManager.GetAllTasks().Where(tTask => tTask.task == task && tTask.connectorName == connectorName);
    else
        return taskManager.GetAllTasks().Where(tTask =>
            tTask.task == task && tTask.connectorName == connectorName && tTask.ToString()
                .Contains(searchString, StringComparison.InvariantCultureIgnoreCase));
});

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

app.MapDelete("/Queue/Dequeue", (string taskType, string? connectorName, string? publicationId) =>
{
    TrangaTask.Task pTask = Enum.Parse<TrangaTask.Task>(taskType);
    TrangaTask? task = taskManager.GetAllTasks().FirstOrDefault(tTask =>
        tTask.task == pTask && tTask.publication?.internalId == publicationId && tTask.connectorName == connectorName);
    if (task is null)
        return;
    taskManager.RemoveTaskFromQueue(task);
});

app.MapGet("/Settings/Get", () => taskManager.settings);

app.MapPost("/Settings/Update", (string? downloadLocation, string? komgaUrl, string? komgaAuth) => taskManager.UpdateSettings(downloadLocation, komgaUrl, komgaAuth) );

app.Run();