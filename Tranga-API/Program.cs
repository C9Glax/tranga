using System.Text.Json;
using Tranga;
using Tranga.Connectors;

TaskManager taskManager = new TaskManager(Directory.GetCurrentDirectory());
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/GetConnectors", () => JsonSerializer.Serialize(taskManager.GetAvailableConnectors().Values.ToArray()));

app.MapGet("/GetPublications", (string connectorName, string? title) =>
{
    Connector? connector = taskManager.GetAvailableConnectors().FirstOrDefault(c => c.Key == connectorName).Value;
    if (connector is null)
        return JsonSerializer.Serialize($"Connector {connectorName} is not a known connector.");

    Publication[] publications;
    if (title is not null)
        publications = connector.GetPublications(title);
    else
        publications = connector.GetPublications();

    return JsonSerializer.Serialize(publications);
});

app.MapGet("/ListTasks", () => JsonSerializer.Serialize(taskManager.GetAllTasks()));

app.MapGet("/CreateTask",
    (TrangaTask.Task task, string connectorName, string? publicationName, TimeSpan reoccurrence, string language) =>
    {
        Publication? publication =
            taskManager.GetAllPublications().FirstOrDefault(pub => pub.downloadUrl == publicationName);
        if (publication is null)
            JsonSerializer.Serialize($"Publication {publicationName} is unknown.");
        
        taskManager.AddTask(task, connectorName, publication, reoccurrence, language);
        JsonSerializer.Serialize("Success");
    });

app.MapGet("/RemoveTask", (TrangaTask.Task task, string connector, string? publicationName) =>
{
    Publication? publication =
        taskManager.GetAllPublications().FirstOrDefault(pub => pub.downloadUrl == publicationName);
    if (publication is null)
        JsonSerializer.Serialize($"Publication {publicationName} is unknown.");
        
    taskManager.RemoveTask(task, connector, publication);
    JsonSerializer.Serialize("Success");
});

app.Run();