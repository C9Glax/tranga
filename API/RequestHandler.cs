using System.Net;
using System.Text.RegularExpressions;
using Tranga;
using Tranga.TrangaTasks;

namespace API;

public class RequestHandler
{
    private TaskManager _taskManager;
    private Server _parent;

    private List<ValueTuple<HttpMethod, string, string[]>> _validRequestPaths = new()
    {
        new(HttpMethod.Get, "/", Array.Empty<string>()),
        new(HttpMethod.Get, "/Connectors", Array.Empty<string>()),
        new(HttpMethod.Get, "/Publications/Known", new[] { "internalId?" }),
        new(HttpMethod.Get, "/Publications/FromConnector", new[] { "connectorName", "title" }),
        new(HttpMethod.Get, "/Publications/Chapters",
            new[] { "connectorName", "internalId", "onlyNew?", "onlyExisting?", "language?" }),
        new(HttpMethod.Get, "/Tasks/Types", Array.Empty<string>()),
        new(HttpMethod.Post, "/Tasks/CreateMonitorTask",
            new[] { "connectorName", "internalId", "reoccurrenceTime", "language?" }),
        //DEPRECATED new(HttpMethod.Post, "/Tasks/CreateUpdateLibraryTask", new[] { "reoccurrenceTime" }),
        new(HttpMethod.Post, "/Tasks/CreateDownloadChaptersTask",
            new[] { "connectorName", "internalId", "chapters", "language?" }),
        new(HttpMethod.Get, "/Tasks", new[] { "taskType", "connectorName?", "publicationId?" }),
        new(HttpMethod.Delete, "/Tasks", new[] { "taskType", "connectorName?", "searchString?" }),
        new(HttpMethod.Get, "/Tasks/Progress",
            new[] { "taskType", "connectorName", "publicationId", "chapterSortNumber?" }),
        new(HttpMethod.Post, "/Tasks/Start", new[] { "taskType", "connectorName?", "internalId?" }),
        new(HttpMethod.Get, "/Tasks/RunningTasks", Array.Empty<string>()),
        new(HttpMethod.Get, "/Queue/List", Array.Empty<string>()),
        new(HttpMethod.Post, "/Queue/Enqueue", new[] { "taskType", "connectorName?", "publicationId?" }),
        new(HttpMethod.Delete, "/Queue/Dequeue", new[] { "taskType", "connectorName?", "publicationId?" }),
        new(HttpMethod.Get, "/Settings", Array.Empty<string>()),
        new(HttpMethod.Post, "/Settings/Update", new[]
        {
            "downloadLocation?", "komgaUrl?", "komgaAuth?", "kavitaUrl?", "kavitaUsername?",
            "kavitaPassword?", "gotifyUrl?", "gotifyAppToken?", "lunaseaWebhook?"
        })
    };

    public RequestHandler(TaskManager taskManager, Server parent)
    {
        this._taskManager = taskManager;
        this._parent = parent;
    }

    internal void HandleRequest(HttpListenerRequest request, HttpListenerResponse response)
    {
        string requestPath = request.Url!.LocalPath;
        if (requestPath.Contains("favicon"))
        {
            _parent.SendResponse(HttpStatusCode.NoContent, response);
            return;
        }
        if (!this._validRequestPaths.Any(path => path.Item1.Method == request.HttpMethod && path.Item2 == requestPath))
        {
            _parent.SendResponse(HttpStatusCode.BadRequest, response);
            return;
        }
        Dictionary<string, string> variables = GetRequestVariables(request.Url!.Query);
        object? responseObject = null;
        switch (request.HttpMethod)
        {
            case "GET":
                responseObject = this.HandleGet(requestPath, variables);
                break;
            case "POST":
                this.HandlePost(requestPath, variables);
                break;
            case "DELETE":
                this.HandleDelete(requestPath, variables);
                break;
        }
        _parent.SendResponse(HttpStatusCode.OK, response, responseObject);
    }

    private Dictionary<string, string> GetRequestVariables(string query)
    {
        Dictionary<string, string> ret = new();
        Regex queryRex = new (@"\?{1}&?([A-z0-9-=]+=[A-z0-9-=]+)+(&[A-z0-9-=]+=[A-z0-9-=]+)*");
        if (!queryRex.IsMatch(query))
            return ret;
        query = query.Substring(1);
        foreach (string kvpair in query.Split('&').Where(str => str.Length >= 3))
        {
            string var = kvpair.Split('=')[0];
            string val = Regex.Replace(kvpair.Substring(var.Length + 1), "%20", " ");
            val = Regex.Replace(val, "%[0-9]{2}", "");
            ret.Add(var, val);
        }
        return ret;
    }

    private void HandleDelete(string requestPath, Dictionary<string, string> variables)
    {
        switch (requestPath)
        {
            case "/Tasks":
                variables.TryGetValue("taskType", out string? taskType1);
                variables.TryGetValue("connectorName", out string? connectorName1);
                variables.TryGetValue("publicationId", out string? publicationId1);
                if(taskType1 is null)
                    return;

                try
                {
                    TrangaTask.Task task = Enum.Parse<TrangaTask.Task>(taskType1);
                    foreach(TrangaTask tTask in _taskManager.GetTasksMatching(task, connectorName1, internalId: publicationId1))
                        _taskManager.DeleteTask(tTask);
                }
                catch (ArgumentException)
                {
                    return;
                }
                break;
            case "/Queue/Dequeue":
                variables.TryGetValue("taskType", out string? taskType2);
                variables.TryGetValue("connectorName", out string? connectorName2);
                variables.TryGetValue("publicationId", out string? publicationId2);
                if(taskType2 is null)
                    return;
                
                try
                {
                    TrangaTask.Task pTask = Enum.Parse<TrangaTask.Task>(taskType2);
                    TrangaTask? task = _taskManager
                        .GetTasksMatching(pTask, connectorName: connectorName2, internalId: publicationId2).FirstOrDefault();
            
                    if (task is null)
                        return;
                    _taskManager.RemoveTaskFromQueue(task);
                }
                catch (ArgumentException)
                {
                    return;
                }
                break;
        }
    }

    private void HandlePost(string requestPath, Dictionary<string, string> variables)
    {
        switch (requestPath)
        {
                
            case "/Tasks/CreateMonitorTask":
                variables.TryGetValue("connectorName", out string? connectorName1);
                variables.TryGetValue("internalId", out string? internalId1);
                variables.TryGetValue("reoccurrenceTime", out string? reoccurrenceTime1);
                variables.TryGetValue("language", out string? language1);
                if (connectorName1 is null || internalId1 is null || reoccurrenceTime1 is null)
                    return;
                Connector? connector1 =
                    _taskManager.GetAvailableConnectors().FirstOrDefault(con => con.Key == connectorName1).Value;
                if (connector1 is null)
                    return;
                Publication? publication1 = _taskManager.GetAllPublications().FirstOrDefault(pub => pub.internalId == internalId1);
                if (!publication1.HasValue)
                    return;
                _taskManager.AddTask(new MonitorPublicationTask(connectorName1, (Publication)publication1, TimeSpan.Parse(reoccurrenceTime1), language1 ?? "en"));
                break;
            case "/Tasks/CreateUpdateLibraryTask": // DEPRECATED
                /*variables.TryGetValue("reoccurrenceTime", out string? reoccurrenceTime2);
                if (reoccurrenceTime2 is null)
                    return;
                _taskManager.AddTask(new UpdateLibrariesTask(TimeSpan.Parse(reoccurrenceTime2)));*/
                break;
            case "/Tasks/CreateDownloadChaptersTask":
                variables.TryGetValue("connectorName", out string? connectorName2);
                variables.TryGetValue("internalId", out string? internalId2);
                variables.TryGetValue("chapters", out string? chapters);
                variables.TryGetValue("language", out string? language2);
                if (connectorName2 is null || internalId2 is null || chapters is null)
                    return;
                Connector? connector2 =
                    _taskManager.GetAvailableConnectors().FirstOrDefault(con => con.Key == connectorName2).Value;
                if (connector2 is null)
                    return;
                Publication? publication2 = _taskManager.GetAllPublications().FirstOrDefault(pub => pub.internalId == internalId2);
                if (publication2 is null)
                    return;
    
                IEnumerable<Chapter> toDownload = connector2.SelectChapters((Publication)publication2, chapters, language2 ?? "en");
                foreach(Chapter chapter in toDownload)
                    _taskManager.AddTask(new DownloadChapterTask(connectorName2, (Publication)publication2, chapter, "en"));
                break;
            case "/Tasks/Start":
                variables.TryGetValue("taskType", out string? taskType1);
                variables.TryGetValue("connectorName", out string? connectorName3);
                variables.TryGetValue("internalId", out string? internalId3);
                if (taskType1 is null)
                    return;
                try
                {
                    TrangaTask.Task pTask = Enum.Parse<TrangaTask.Task>(taskType1);
                    TrangaTask? task = _taskManager
                        .GetTasksMatching(pTask, connectorName: connectorName3, internalId: internalId3).FirstOrDefault();
            
                    if (task is null)
                        return;
                    _taskManager.ExecuteTaskNow(task);
                }
                catch (ArgumentException)
                {
                    return;
                }
                break;
            case "/Queue/Enqueue":
                variables.TryGetValue("taskType", out string? taskType2);
                variables.TryGetValue("connectorName", out string? connectorName4);
                variables.TryGetValue("publicationId", out string? publicationId);
                if (taskType2 is null)
                    return;
                try
                {
                    TrangaTask.Task pTask = Enum.Parse<TrangaTask.Task>(taskType2);
                    TrangaTask? task = _taskManager
                        .GetTasksMatching(pTask, connectorName: connectorName4, internalId: publicationId).FirstOrDefault();
            
                    if (task is null)
                        return;
                    _taskManager.AddTaskToQueue(task);
                }
                catch (ArgumentException)
                {
                    return;
                }
                break;
            case "/Settings/Update":
                variables.TryGetValue("downloadLocation", out string? downloadLocation);
                variables.TryGetValue("komgaUrl", out string? komgaUrl);
                variables.TryGetValue("komgaAuth", out string? komgaAuth);
                variables.TryGetValue("kavitaUrl", out string? kavitaUrl);
                variables.TryGetValue("kavitaUsername", out string? kavitaUsername);
                variables.TryGetValue("kavitaPassword", out string? kavitaPassword);
                variables.TryGetValue("gotifyUrl", out string? gotifyUrl);
                variables.TryGetValue("gotifyAppToken", out string? gotifyAppToken);
                variables.TryGetValue("lunaseaWebhook", out string? lunaseaWebhook);
                
                if (downloadLocation is not null && downloadLocation.Length > 0)
                    _taskManager.settings.UpdateSettings(TrangaSettings.UpdateField.DownloadLocation, _parent.logger, downloadLocation);
                if (komgaUrl is not null && komgaAuth is not null && komgaUrl.Length > 5 && komgaAuth.Length > 0)
                    _taskManager.settings.UpdateSettings(TrangaSettings.UpdateField.Komga, _parent.logger, komgaUrl, komgaAuth);
                if (kavitaUrl is not null && kavitaPassword is not null && kavitaUsername is not null && kavitaUrl.Length > 5 &&
                    kavitaUsername.Length > 0 && kavitaPassword.Length > 0)
                    _taskManager.settings.UpdateSettings(TrangaSettings.UpdateField.Kavita, _parent.logger, kavitaUrl, kavitaUsername,
                        kavitaPassword);
                if (gotifyUrl is not null && gotifyAppToken is not null && gotifyUrl.Length > 5 && gotifyAppToken.Length > 0)
                    _taskManager.settings.UpdateSettings(TrangaSettings.UpdateField.Gotify, _parent.logger, gotifyUrl, gotifyAppToken);
                if(lunaseaWebhook is not null && lunaseaWebhook.Length > 5)
                    _taskManager.settings.UpdateSettings(TrangaSettings.UpdateField.LunaSea, _parent.logger, lunaseaWebhook);
                break;
        }
    }

    private object? HandleGet(string requestPath, Dictionary<string, string> variables)
    {
        switch (requestPath)
        {
            case "/Connectors":
                return this._taskManager.GetAvailableConnectors().Keys.ToArray();
            case "/Publications/Known":
                variables.TryGetValue("internalId", out string? internalId1);
                if(internalId1 is null)
                    return _taskManager.GetAllPublications();
                return new [] { _taskManager.GetAllPublications().FirstOrDefault(pub => pub.internalId == internalId1) };
            case "/Publications/FromConnector":
                variables.TryGetValue("connectorName", out string? connectorName1);
                variables.TryGetValue("title", out string? title);
                if (connectorName1 is null || title is null)
                    return null;
                Connector? connector1 = _taskManager.GetAvailableConnectors().FirstOrDefault(con => con.Key == connectorName1).Value;
                if (connector1 is null)
                    return null;
                if(title.Length < 4)
                    return null;
                return connector1.GetPublications(ref _taskManager.collection, title);
            case "/Publications/Chapters":
                string[] yes = { "true", "yes", "1", "y" };
                variables.TryGetValue("connectorName", out string? connectorName2);
                variables.TryGetValue("internalId", out string? internalId2);
                variables.TryGetValue("onlyNew", out string? onlyNew);
                variables.TryGetValue("onlyExisting", out string? onlyExisting);
                variables.TryGetValue("language", out string? language);
                if (connectorName2 is null || internalId2 is null)
                    return null;
                bool newOnly = onlyNew is not null && yes.Contains(onlyNew);
                bool existingOnly = onlyExisting is not null && yes.Contains(onlyExisting);
        
                Connector? connector2 = _taskManager.GetAvailableConnectors().FirstOrDefault(con => con.Key == connectorName2).Value;
                if (connector2 is null)
                    return null;
                Publication? publication = _taskManager.GetAllPublications().FirstOrDefault(pub => pub.internalId == internalId2);
                if (publication is null)
                    return null;
        
                if(newOnly)
                    return connector2.GetNewChaptersList((Publication)publication, language??"en", ref _taskManager.collection).ToArray();
                else if (existingOnly)
                    return _taskManager.GetExistingChaptersList(connector2, (Publication)publication, language ?? "en").ToArray();
                else
                    return connector2.GetChapters((Publication)publication, language??"en");
            case "/Tasks/Types":
                return Enum.GetNames(typeof(TrangaTask.Task));
            case "/Tasks":
                variables.TryGetValue("taskType", out string? taskType1);
                variables.TryGetValue("connectorName", out string? connectorName3);
                variables.TryGetValue("searchString", out string? searchString);
                if (taskType1 is null)
                    return null;
                try
                {
                    TrangaTask.Task task = Enum.Parse<TrangaTask.Task>(taskType1);
                    return _taskManager.GetTasksMatching(task, connectorName:connectorName3, searchString:searchString);
                }
                catch (ArgumentException)
                {
                    return null;
                }
            case "/Tasks/Progress":
                variables.TryGetValue("taskType", out string? taskType2);
                variables.TryGetValue("connectorName", out string? connectorName4);
                variables.TryGetValue("publicationId", out string? publicationId);
                variables.TryGetValue("chapterNumber", out string? chapterNumber);
                if (taskType2 is null || connectorName4 is null || publicationId is null)
                    return null;
                Connector? connector =
                    _taskManager.GetAvailableConnectors().FirstOrDefault(con => con.Key == connectorName4).Value;
                if (connector is null)
                    return null;
                try
                {
                    TrangaTask? task = null;
                    TrangaTask.Task pTask = Enum.Parse<TrangaTask.Task>(taskType2);
                    if (pTask is TrangaTask.Task.MonitorPublication)
                    {
                        task = _taskManager.GetTasksMatching(pTask, connectorName: connectorName4, internalId: publicationId).FirstOrDefault();
                    }else if (pTask is TrangaTask.Task.DownloadChapter && chapterNumber is not null)
                    {
                        task = _taskManager.GetTasksMatching(pTask, connectorName: connectorName4, internalId: publicationId,
                            chapterNumber: chapterNumber).FirstOrDefault();
                    }
                    if (task is null)
                        return null;

                    return task.progress;
                }
                catch (ArgumentException)
                {
                    return null;
                }
            case "/Tasks/RunningTasks":
                return _taskManager.GetAllTasks().Where(task => task.state is TrangaTask.ExecutionState.Running);
            case "/Queue/List":
                return _taskManager.GetAllTasks().Where(task => task.state is TrangaTask.ExecutionState.Enqueued).OrderBy(task => task.nextExecution);
            case "/Settings":
                return _taskManager.settings;
            case "/":
            default:
                return this._validRequestPaths;
        }
    }
}