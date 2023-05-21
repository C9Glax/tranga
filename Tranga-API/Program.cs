using System.Text.Json;
using Tranga;

TaskManager taskManager = new (Directory.GetCurrentDirectory());
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

app.MapGet("/CreateTask",
    (TrangaTask.Task task, string? connectorName, string? publicationName, TimeSpan reoccurrence, string? language) =>
    {
        switch (task)
        {
            case TrangaTask.Task.UpdateKomgaLibrary:
                taskManager.AddTask(TrangaTask.Task.UpdateKomgaLibrary, null, null, reoccurrence);
                break;
            case TrangaTask.Task.DownloadNewChapters:
                try
                {
                    Connector connector = taskManager.GetConnector(connectorName);
                    
                    Publication[] publications;
                    if (publicationName is not null)
                        publications = connector.GetPublications(publicationName);
                    else
                        publications = connector.GetPublications();
                    
                    Publication? publication = publications.FirstOrDefault(pub => pub.downloadUrl == publicationName);
                    if (publication is null)
                        JsonSerializer.Serialize($"Publication {publicationName} is unknown.");
                    taskManager.AddTask(TrangaTask.Task.DownloadNewChapters, connectorName, publication, reoccurrence, language ?? "");
                    return JsonSerializer.Serialize("Success");
                }
                catch (Exception e)
                {
                    return JsonSerializer.Serialize(e.Message);
                }

            default: return JsonSerializer.Serialize("Not Implemented");
        }

        return JsonSerializer.Serialize("Not Implemented");
    });

app.MapGet("/RemoveTask", (TrangaTask.Task task, string? connectorName, string? publicationName) =>
{
    switch (task)
    {
        case TrangaTask.Task.UpdateKomgaLibrary:
            taskManager.RemoveTask(TrangaTask.Task.UpdateKomgaLibrary, null, null);
            return JsonSerializer.Serialize("Success");
        case TrangaTask.Task.DownloadNewChapters:
            Publication? publication = taskManager.GetAllPublications().FirstOrDefault(pub => pub.downloadUrl == publicationName);
            if (publication is null)
                JsonSerializer.Serialize($"Publication {publicationName} is unknown.");
            
            taskManager.RemoveTask(TrangaTask.Task.DownloadNewChapters, connectorName, publication);
            
            return JsonSerializer.Serialize("Success");
        
        default: return JsonSerializer.Serialize("Not Implemented");
    }
});

app.Run();