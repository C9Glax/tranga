using System.Runtime.InteropServices;
using Logging;
using Tranga;
using Tranga.TrangaTasks;

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
    settings = new TrangaSettings(downloadFolderPath, applicationFolderPath, new HashSet<LibraryManager>(), new HashSet<NotificationManager>());

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
foreach(NotificationManager nm in taskManager.settings.notificationManagers)
    nm.SendNotification("Tranga-API", "Started Tranga-API");

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

app.MapGet("/Controllers/Get", () =>  taskManager.GetAvailableConnectors().Keys.ToArray());

app.MapGet("/Publications/GetKnown", (string? internalId) =>
{
    if(internalId is null)
        return taskManager.GetAllPublications();
    
    return new [] { taskManager.GetAllPublications().FirstOrDefault(pub => pub.internalId == internalId) };
});

app.MapGet("/Publications/GetFromConnector", (string connectorName, string title) =>
{
    Connector? connector = taskManager.GetAvailableConnectors().FirstOrDefault(con => con.Key == connectorName).Value;
    if (connector is null)
        return Array.Empty<Publication>();
    if(title.Length < 4)
        return Array.Empty<Publication>();
    return taskManager.GetPublicationsFromConnector(connector, title);
});

app.MapGet("/Publications/GetChapters",
    (string connectorName, string internalId, string? onlyNew, string? onlyExisting, string? language) =>
    { 
        string[] yes = { "true", "yes", "1", "y" };
        bool newOnly = onlyNew is not null && yes.Contains(onlyNew);
        bool existingOnly = onlyExisting is not null && yes.Contains(onlyExisting);
        
        Connector? connector = taskManager.GetAvailableConnectors().FirstOrDefault(con => con.Key == connectorName).Value;
        if (connector is null)
            return Array.Empty<Chapter>();
        Publication? publication = taskManager.GetAllPublications().FirstOrDefault(pub => pub.internalId == internalId);
        if (publication is null)
            return Array.Empty<Chapter>();
        
        if(newOnly)
            return taskManager.GetNewChaptersList(connector, (Publication)publication, language??"en").ToArray();
        else if (existingOnly)
            return taskManager.GetExistingChaptersList(connector, (Publication)publication, language ?? "en").ToArray();
        else
            return connector.GetChapters((Publication)publication, language??"en");
});

app.MapGet("/Tasks/GetTypes", () => Enum.GetNames(typeof(TrangaTask.Task)));


app.MapPost("/Tasks/CreateMonitorTask",
    (string connectorName, string internalId, string reoccurrenceTime, string? language) =>
    {
        Connector? connector =
            taskManager.GetAvailableConnectors().FirstOrDefault(con => con.Key == connectorName).Value;
        if (connector is null)
            return;
        Publication? publication = taskManager.GetAllPublications().FirstOrDefault(pub => pub.internalId == internalId);
        if (publication is null)
            return;
        taskManager.AddTask(new MonitorPublicationTask(connectorName, (Publication)publication, TimeSpan.Parse(reoccurrenceTime), language ?? "en"));
    });

app.MapPost("/Tasks/CreateUpdateLibraryTask", (string reoccurrenceTime) =>
{
    taskManager.AddTask(new UpdateLibrariesTask(TimeSpan.Parse(reoccurrenceTime)));
});

app.MapPost("/Tasks/CreateDownloadChaptersTask", (string connectorName, string internalId, string chapters, string? language) => {
    
    Connector? connector =
        taskManager.GetAvailableConnectors().FirstOrDefault(con => con.Key == connectorName).Value;
    if (connector is null)
        return;
    Publication? publication = taskManager.GetAllPublications().FirstOrDefault(pub => pub.internalId == internalId);
    if (publication is null)
        return;
    
    IEnumerable<Chapter> toDownload = connector.SearchChapters((Publication)publication, chapters, language ?? "en");
    foreach(Chapter chapter in toDownload)
        taskManager.AddTask(new DownloadChapterTask(connectorName, (Publication)publication, chapter, "en"));
});

app.MapDelete("/Tasks/Delete", (string taskType, string? connectorName, string? publicationId) =>
{
    TrangaTask.Task task = Enum.Parse<TrangaTask.Task>(taskType);
    foreach(TrangaTask tTask in taskManager.GetTasksMatching(task, connectorName, internalId: publicationId))
        taskManager.DeleteTask(tTask);
});

app.MapGet("/Tasks/Get", (string taskType, string? connectorName, string? searchString) =>
{
    try
    {
        TrangaTask.Task task = Enum.Parse<TrangaTask.Task>(taskType);
        return taskManager.GetTasksMatching(task, connectorName:connectorName, searchString:searchString);
    }
    catch (ArgumentException)
    {
        return Array.Empty<TrangaTask>();
    }
});

app.MapGet("/Tasks/GetProgress", (string taskType, string connectorName, string publicationId, string? chapterSortNumber) =>
{
    Connector? connector =
        taskManager.GetAvailableConnectors().FirstOrDefault(con => con.Key == connectorName).Value;
    if (connector is null)
        return -1f;
    try
    {
        TrangaTask? task = null;
        TrangaTask.Task pTask = Enum.Parse<TrangaTask.Task>(taskType);
        if (pTask is TrangaTask.Task.MonitorPublication)
        {
            task = taskManager.GetTasksMatching(pTask, connectorName: connectorName, internalId: publicationId).FirstOrDefault();
        }else if (pTask is TrangaTask.Task.DownloadChapter && chapterSortNumber is not null)
        {
            task = taskManager.GetTasksMatching(pTask, connectorName: connectorName, internalId: publicationId,
                chapterSortNumber: chapterSortNumber).FirstOrDefault();
        }
        if (task is null)
            return -1f;

        return task.progress;
    }
    catch (ArgumentException)
    {
        return -1f;
    }
});

app.MapPost("/Tasks/Start", (string taskType, string? connectorName, string? internalId) =>
{
    try
    {
        TrangaTask.Task pTask = Enum.Parse<TrangaTask.Task>(taskType);
        TrangaTask? task = taskManager
            .GetTasksMatching(pTask, connectorName: connectorName, internalId: internalId).FirstOrDefault();
            
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
    () => taskManager.GetAllTasks().Where(task => task.state is TrangaTask.ExecutionState.Enqueued).OrderBy(task => task.nextExecution));

app.MapPost("/Queue/Enqueue", (string taskType, string? connectorName, string? publicationId) =>
{
    try
    {
        TrangaTask.Task pTask = Enum.Parse<TrangaTask.Task>(taskType);
        TrangaTask? task = taskManager
            .GetTasksMatching(pTask, connectorName: connectorName, internalId: publicationId).FirstOrDefault();
            
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
        TrangaTask? task = taskManager
            .GetTasksMatching(pTask, connectorName: connectorName, internalId: publicationId).FirstOrDefault();
            
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
    (string? downloadLocation, string? komgaUrl, string? komgaAuth, string? kavitaUrl, string? kavitaUsername,
        string? kavitaPassword, string? gotifyUrl, string? gotifyAppToken, string? lunaseaWebhook) =>
    {
        if (downloadLocation is not null && downloadLocation.Length > 0)
            taskManager.settings.UpdateSettings(TrangaSettings.UpdateField.DownloadLocation, logger, downloadLocation);
        if (komgaUrl is not null && komgaAuth is not null && komgaUrl.Length > 5 && komgaAuth.Length > 0)
            taskManager.settings.UpdateSettings(TrangaSettings.UpdateField.Komga, logger, komgaUrl, komgaAuth);
        if (kavitaUrl is not null && kavitaPassword is not null && kavitaUsername is not null && kavitaUrl.Length > 5 &&
            kavitaUsername.Length > 0 && kavitaPassword.Length > 0)
            taskManager.settings.UpdateSettings(TrangaSettings.UpdateField.Kavita, logger, kavitaUrl, kavitaUsername,
                kavitaPassword);
        if (gotifyUrl is not null && gotifyAppToken is not null && gotifyUrl.Length > 5 && gotifyAppToken.Length > 0)
            taskManager.settings.UpdateSettings(TrangaSettings.UpdateField.Gotify, logger, gotifyUrl, gotifyAppToken);
        if(lunaseaWebhook is not null && lunaseaWebhook.Length > 5)
            taskManager.settings.UpdateSettings(TrangaSettings.UpdateField.LunaSea, logger, lunaseaWebhook);
    });

app.Run();