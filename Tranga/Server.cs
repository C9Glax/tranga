using System.Data;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Tranga.Jobs;
using Tranga.LibraryConnectors;
using Tranga.MangaConnectors;
using Tranga.NotificationConnectors;

namespace Tranga;

public class Server : GlobalBase
{
    private readonly HttpListener _listener = new ();
    private readonly Tranga _parent;
    
    public Server(Tranga parent) : base(parent)
    {
        this._parent = parent;
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            this._listener.Prefixes.Add($"http://*:{settings.apiPortNumber}/");
        else
            this._listener.Prefixes.Add($"http://localhost:{settings.apiPortNumber}/");
        Thread listenThread = new (Listen);
        listenThread.Start();
        Thread watchThread = new(WatchRunning);
        watchThread.Start();
    }

    private void WatchRunning()
    {
        while(_parent.keepRunning)
            Thread.Sleep(1000);
        this._listener.Close();
    }

    private void Listen()
    {
        this._listener.Start();
        foreach(string prefix in this._listener.Prefixes)
            Log($"Listening on {prefix}");
        while (this._listener.IsListening && _parent.keepRunning)
        {
            try
            {
                HttpListenerContext context = this._listener.GetContext();
                //Log($"{context.Request.HttpMethod} {context.Request.Url} {context.Request.UserAgent}");
                Task t = new(() =>
                {
                    HandleRequest(context);
                });
                t.Start();
            }
            catch (HttpListenerException)
            {
                
            }
        }
    }

    private void HandleRequest(HttpListenerContext context)
    {
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;
        if(request.HttpMethod == "OPTIONS")
            SendResponse(HttpStatusCode.OK, context.Response);
        if(request.Url!.LocalPath.Contains("favicon"))
            SendResponse(HttpStatusCode.NoContent, response);

        switch (request.HttpMethod)
        {
            case "GET":
                HandleGet(request, response);
                break;
            case "POST":
                HandlePost(request, response);
                break;
            case "DELETE":
                HandleDelete(request, response);
                break;
            default: 
                SendResponse(HttpStatusCode.BadRequest, response);
                break;
        }
    }
    
    private Dictionary<string, string> GetRequestVariablesFromUri(string query)
    {
        Dictionary<string, string> ret = new();
        Regex queryRex = new (@"\?{1}&?([A-z0-9-=]+=[A-z0-9-=]+)+(&[A-z0-9-=]+=[A-z0-9-=]+)*");
        if (!queryRex.IsMatch(query))
            return ret;
        query = query.Substring(1);
        foreach (string keyValuePair in query.Split('&').Where(str => str.Length >= 3))
        {
            string var = keyValuePair.Split('=')[0];
            string val = Regex.Replace(keyValuePair.Substring(var.Length + 1), "%20", " ");
            val = Regex.Replace(val, "%[0-9]{2}", "");
            ret.Add(var, val);
        }
        return ret;
    }

    private Dictionary<string, JsonElement> GetRequestVariables(HttpListenerRequest message)
    {
        Dictionary<string, JsonElement> variables = new();
        Encoding encoding = message.ContentEncoding;
        if(encoding is not UTF8Encoding)
        {
            Log($"Request Encoding is not UTF8 ({encoding})");
            return variables;
        }
        long length = message.ContentLength64;
        if (length < 1)
            return variables;
        byte[] buffer = new byte[length];
        int readBytes = message.InputStream.Read(buffer, 0, (int)length);
        if (readBytes != length)
        {
            Log($"Not all content could be read. Read {readBytes}, MessageLength {length}");
            return variables;
        }
        JsonNode? content = JsonNode.Parse(buffer);
        if (content is null)
        {
            Log($"Json could not be parsed.\n{buffer}");
            return variables;
        }
        foreach((string key, JsonNode? value) in content.AsObject())
            if(value is not null)
                variables.Add(key, value.GetValue<JsonElement>());
        
        return variables;
    }

    private T GetRequestVariable<T>(Dictionary<string, JsonElement> variables, string key)
    {
        if (!variables.ContainsKey(key))
            throw new KeyNotFoundException($"Missing value: {key}");
        if (typeof(T) == typeof(string) && variables[key].ValueKind == JsonValueKind.String &&
            variables[key].GetString() is { } s)
            return (T)(object)s;
        if (typeof(T) == typeof(int) && variables[key].ValueKind == JsonValueKind.Number && variables[key].TryGetInt32(out int i32))
            return (T)(object)i32;
        if (typeof(T) == typeof(uint) && variables[key].ValueKind == JsonValueKind.Number && variables[key].TryGetUInt32(out uint ui32))
            return (T)(object)ui32;
        if (typeof(T) == typeof(bool) && variables[key].ValueKind == JsonValueKind.False)
            return (T)(object)false;
        if (typeof(T) == typeof(bool) && variables[key].ValueKind == JsonValueKind.True)
            return (T)(object)true;
        if (typeof(T) == typeof(float) && variables[key].ValueKind == JsonValueKind.Number && variables[key].TryGetSingle(out float f))
            return (T)(object)f;
        if (typeof(T) == typeof(double) && variables[key].ValueKind == JsonValueKind.Number && variables[key].TryGetDouble(out double d))
            return (T)(object)d;
        if (typeof(T) == typeof(TimeSpan) && variables[key].ValueKind == JsonValueKind.String &&
            TimeSpan.TryParse(GetRequestVariable<string>(variables, key), out TimeSpan ts))
            return (T)(object)ts;
        if (typeof(T) == typeof(DateTime) && variables[key].ValueKind == JsonValueKind.String &&
            DateTime.TryParse(GetRequestVariable<string>(variables, key), out DateTime dt))
            return (T)(object)dt;

        throw new ConstraintException($"{key} was not of Type {typeof(T).FullName} ({variables[key].GetType()}) or no conversion could be found.");
    }

    private void HandleGet(HttpListenerRequest request, HttpListenerResponse response)
    {
        Dictionary<string, JsonElement> requestVariables = GetRequestVariables(request);
        string path = Regex.Match(request.Url!.LocalPath, @"[A-z0-9]+(\/[A-z0-9]+)*").Value;
        try
        {
            string? connectorName, jobId, internalId;
            MangaConnector? connector;
            Manga? manga;
            switch (path)
            {
                case "Connectors":
                    SendResponse(HttpStatusCode.OK, response,
                        _parent.GetConnectors().Select(con => con.name).ToArray());
                    break;
                case "Manga/Cover":
                    internalId = GetRequestVariable<string>(requestVariables, "internalId");
                    if (!_parent.TryGetPublicationById(internalId, out manga))
                    {
                        SendResponse(HttpStatusCode.NotFound, response, $"internalId {internalId} not found.");
                        break;
                    }

                    string filePath = settings.GetFullCoverPath((Manga)manga!);
                    if (File.Exists(filePath))
                    {
                        FileStream coverStream = new(filePath, FileMode.Open);
                        SendResponse(HttpStatusCode.OK, response, coverStream);
                    }
                    else
                    {
                        SendResponse(HttpStatusCode.NotFound, response);
                    }

                    break;
                case "Manga/FromConnector":
                    string? title = null, url = null;
                    try
                    {
                        title = GetRequestVariable<string>(requestVariables, "title");
                    }
                    catch (KeyNotFoundException) { }
                    try
                    {
                        url = GetRequestVariable<string>(requestVariables, "url");
                    }
                    catch (KeyNotFoundException) { }
                    
                    if(title is null && url is null)
                    {
                        SendResponse(HttpStatusCode.BadRequest, response, "Missing keys title or url");
                        break;
                    }

                    connectorName = GetRequestVariable<string>(requestVariables, "connector");
                    if (!_parent.TryGetConnector(connectorName, out connector))
                    {
                        SendResponse(HttpStatusCode.BadRequest, response, $"connector {connectorName} does not exist.");
                        break;
                    }

                    if (url is not null)
                    {
                        HashSet<Manga> ret = new();
                        manga = connector!.GetMangaFromUrl(url);
                        if (manga is not null)
                            ret.Add((Manga)manga);
                        SendResponse(HttpStatusCode.OK, response, ret);
                    }
                    else
                        SendResponse(HttpStatusCode.OK, response, connector!.GetManga(title!));

                    break;
                case "Manga/Chapters":
                    connectorName = GetRequestVariable<string>(requestVariables, "connector");
                    internalId = GetRequestVariable<string>(requestVariables, "internalId");
                    if (!_parent.TryGetConnector(connectorName, out connector))
                    {
                        SendResponse(HttpStatusCode.BadRequest, response, $"connector {connectorName} does not exist.");
                        break;
                    }
                    if (!_parent.TryGetPublicationById(internalId, out manga))
                    {
                        SendResponse(HttpStatusCode.NotFound, response, $"internalId {internalId} not found.");
                        break;
                    }

                    try
                    {
                        string translatedLanguage = GetRequestVariable<string>(requestVariables, "translatedLanguage");
                        SendResponse(HttpStatusCode.OK, response, connector!.GetChapters((Manga)manga!, translatedLanguage));
                    }
                    catch (KeyNotFoundException)
                    {
                        SendResponse(HttpStatusCode.OK, response, connector!.GetChapters((Manga)manga!));
                    }
                    break;
                case "Jobs":
                    try
                    {
                        jobId = GetRequestVariable<string>(requestVariables, "jobId");
                        Job? job = _parent.jobBoss.jobs.FirstOrDefault(jjob => jjob.id == jobId);
                        if (job is null)
                            SendResponse(HttpStatusCode.NotFound, response, $"jobId {jobId} not found.");
                        else
                            SendResponse(HttpStatusCode.OK, response, job);
                    }
                    catch (KeyNotFoundException)
                    {
                        SendResponse(HttpStatusCode.OK, response, _parent.jobBoss.jobs);
                    }
                    break;
                case "Jobs/Progress":
                    jobId = GetRequestVariable<string>(requestVariables, "jobId");
                    Job? job2 = _parent.jobBoss.jobs.FirstOrDefault(jjob => jjob.id == jobId);
                    if (job2 is null)
                        SendResponse(HttpStatusCode.NotFound, response, $"jobId {jobId} not found.");
                    else
                        SendResponse(HttpStatusCode.OK, response, job2.progressToken);
                    break;
                case "Jobs/Running":
                    SendResponse(HttpStatusCode.OK, response,
                        _parent.jobBoss.jobs.Where(jjob => jjob.progressToken.state is ProgressToken.State.Running));
                    break;
                case "Jobs/Waiting":
                    SendResponse(HttpStatusCode.OK, response,
                        _parent.jobBoss.jobs.Where(jjob => jjob.progressToken.state is ProgressToken.State.Standby)
                            .OrderBy(jjob => jjob.nextExecution));
                    break;
                case "Jobs/MonitorJobs":
                    SendResponse(HttpStatusCode.OK, response,
                        _parent.jobBoss.jobs.Where(jjob => jjob is DownloadNewChapters)
                            .OrderBy(jjob => ((DownloadNewChapters)jjob).manga.sortName));
                    break;
                case "Settings":
                    SendResponse(HttpStatusCode.OK, response, settings);
                    break;
                case "Settings/userAgent":
                    SendResponse(HttpStatusCode.OK, response, settings.userAgent);
                    break;
                case "Settings/customRequestLimit":
                    SendResponse(HttpStatusCode.OK, response, settings.requestLimits);
                    break;
                case "Settings/AprilFoolsMode":
                    SendResponse(HttpStatusCode.OK, response, settings.aprilFoolsMode);
                    break;
                case "NotificationConnectors":
                    SendResponse(HttpStatusCode.OK, response, notificationConnectors);
                    break;
                case "NotificationConnectors/Types":
                    SendResponse(HttpStatusCode.OK, response,
                        Enum.GetValues<NotificationConnector.NotificationConnectorType>()
                            .Select(nc => new KeyValuePair<byte, string?>((byte)nc, Enum.GetName(nc))));
                    break;
                case "LibraryConnectors":
                    SendResponse(HttpStatusCode.OK, response, libraryConnectors);
                    break;
                case "LibraryConnectors/Types":
                    SendResponse(HttpStatusCode.OK, response,
                        Enum.GetValues<LibraryConnector.LibraryType>()
                            .Select(lc => new KeyValuePair<byte, string?>((byte)lc, Enum.GetName(lc))));
                    break;
                case "Ping":
                    SendResponse(HttpStatusCode.OK, response, "Pong");
                    break;
                case "LogMessages":
                    if (logger is null || !File.Exists(logger?.logFilePath))
                    {
                        SendResponse(HttpStatusCode.NotFound, response);
                        break;
                    }

                    try
                    {
                        uint messageCount = GetRequestVariable<uint>(requestVariables, "count");
                        try
                        {
                            SendResponse(HttpStatusCode.OK, response, logger.Tail(messageCount));
                        }
                        catch (FormatException f)
                        {
                            SendResponse(HttpStatusCode.InternalServerError, response, f.Message);
                        }
                    }
                    catch (KeyNotFoundException)
                    {
                        SendResponse(HttpStatusCode.OK, response, logger.GetLog());
                    }
                    break;
                case "LogFile":
                    if (logger is null || !File.Exists(logger?.logFilePath))
                    {
                        SendResponse(HttpStatusCode.NotFound, response, "No LogFile.");
                        break;
                    }

                    string logDir = new FileInfo(logger.logFilePath).DirectoryName!;
                    string tmpFilePath = Path.Join(logDir, "Tranga.log");
                    File.Copy(logger.logFilePath, tmpFilePath);
                    SendResponse(HttpStatusCode.OK, response, new FileStream(tmpFilePath, FileMode.Open));
                    File.Delete(tmpFilePath);
                    break;
                default:
                    SendResponse(HttpStatusCode.NotFound, response, "Request-URI doesn't exist.");
                    break;
            }
        }
        catch (Exception e)
        {
            SendResponse(HttpStatusCode.BadRequest, response, e.Message);
        }
    }

    private void HandlePost(HttpListenerRequest request, HttpListenerResponse response)
    {
        Dictionary<string, JsonElement> requestVariables = GetRequestVariables(request);
        string path = Regex.Match(request.Url!.LocalPath, @"[A-z0-9]+(\/[A-z0-9]+)*").Value;
        try
        {
            NotificationConnector.NotificationConnectorType notificationConnectorType;
            LibraryConnector.LibraryType libraryConnectorType;
            string? connectorName, internalId, jobId, customFolderName, translatedLanguage, notificationConnectorStr, libraryConnectorStr;
            MangaConnector? connector;
            Manga? tmpManga;
            Manga manga;
            Job? job;
            switch (path)
            {
                case "Manga":
                    internalId = GetRequestVariable<string>(requestVariables, "internalId");
                    if (!_parent.TryGetPublicationById(internalId, out tmpManga))
                    {
                        SendResponse(HttpStatusCode.NotFound, response, $"internalId {internalId} not found.");
                        break;
                    }

                    manga = (Manga)tmpManga!;
                    SendResponse(HttpStatusCode.OK, response, manga);
                    break;
                case "Jobs/MonitorManga":
                    connectorName = GetRequestVariable<string>(requestVariables, "connector");
                    internalId = GetRequestVariable<string>(requestVariables, "internalId");
                    TimeSpan interval = GetRequestVariable<TimeSpan>(requestVariables, "interval");
                    if (!_parent.TryGetConnector(connectorName, out connector))
                    {
                        SendResponse(HttpStatusCode.BadRequest, response, $"connector {connectorName} does not exist.");
                        break;
                    }
                    if (!_parent.TryGetPublicationById(internalId, out tmpManga))
                    {
                        SendResponse(HttpStatusCode.NotFound, response, $"internalId {internalId} not found.");
                        break;
                    }

                    manga = (Manga)tmpManga!;

                    try
                    {
                        float chapterNum = GetRequestVariable<float>(requestVariables, "ignoreBelowChapterNum");
                        manga.ignoreChaptersBelow = chapterNum;
                    }
                    catch (KeyNotFoundException)
                    {
                    }

                    try
                    {
                        customFolderName = GetRequestVariable<string>(requestVariables, "customFolderName");
                        manga.MovePublicationFolder(settings.downloadLocation, customFolderName);
                    }
                    catch (KeyNotFoundException)
                    {
                    }

                    try
                    {
                        translatedLanguage = GetRequestVariable<string>(requestVariables, "translatedLanguage");
                        _parent.jobBoss.AddJob(new DownloadNewChapters(this, connector!, manga, true, interval,
                            translatedLanguage: translatedLanguage));
                    }
                    catch (KeyNotFoundException)
                    {
                        _parent.jobBoss.AddJob(new DownloadNewChapters(this, connector!, manga, true, interval,
                            translatedLanguage: "en"));
                    }

                    SendResponse(HttpStatusCode.Accepted, response);
                    break;
                case "Jobs/DownloadNewChapters":
                    connectorName = GetRequestVariable<string>(requestVariables, "connector");
                    internalId = GetRequestVariable<string>(requestVariables, "internalId");
                    if (!_parent.TryGetConnector(connectorName, out connector))
                    {
                        SendResponse(HttpStatusCode.BadRequest, response, $"connector {connectorName} does not exist.");
                        break;
                    }
                    if (!_parent.TryGetPublicationById(internalId, out tmpManga))
                    {
                        SendResponse(HttpStatusCode.NotFound, response, $"internalId {internalId} not found.");
                        break;
                    }

                    manga = (Manga)tmpManga!;

                    try
                    {
                        float chapterNum = GetRequestVariable<float>(requestVariables, "ignoreBelowChapterNum");
                        manga.ignoreChaptersBelow = chapterNum;
                    }
                    catch (KeyNotFoundException)
                    {
                    }

                    try
                    {
                        customFolderName = GetRequestVariable<string>(requestVariables, "customFolderName");
                        manga.MovePublicationFolder(settings.downloadLocation, customFolderName);
                    }
                    catch (KeyNotFoundException)
                    {
                    }

                    try
                    {
                        translatedLanguage = GetRequestVariable<string>(requestVariables, "translatedLanguage");
                        _parent.jobBoss.AddJob(new DownloadNewChapters(this, connector!, manga, true,
                            translatedLanguage: translatedLanguage));
                    }
                    catch (KeyNotFoundException)
                    {
                        _parent.jobBoss.AddJob(new DownloadNewChapters(this, connector!, manga, true,
                            translatedLanguage: "en"));
                    }

                    SendResponse(HttpStatusCode.Accepted, response);
                    break;
                case "Jobs/UpdateMetadata":
                    try
                    {
                        internalId = GetRequestVariable<string>(requestVariables, "internalId");
                        Job[] possibleDncJobs = _parent.jobBoss.GetJobsLike(internalId: internalId).ToArray();
                        switch (possibleDncJobs.Length)
                        {
                            case < 1:
                                SendResponse(HttpStatusCode.NotFound, response, "Could not find matching release");
                                break;
                            case > 1:
                                SendResponse(HttpStatusCode.Ambiguous, response, "Multiple releases??");
                                break;
                            default:
                                DownloadNewChapters dncJob = possibleDncJobs[0] as DownloadNewChapters ??
                                                             throw new Exception("Has to be DownloadNewChapters Job");
                                _parent.jobBoss.AddJob(new UpdateMetadata(this, dncJob.mangaConnector, dncJob.manga));
                                SendResponse(HttpStatusCode.Accepted, response);
                                break;
                        }
                    }
                    catch (KeyNotFoundException)
                    {
                        foreach (Job pJob in _parent.jobBoss.jobs.Where(possibleDncJob =>
                                         possibleDncJob.jobType is Job.JobType.DownloadNewChaptersJob)
                                     .ToArray()) //ToArray to avoid modyifying while adding new jobs
                        {
                            DownloadNewChapters dncJob = pJob as DownloadNewChapters ??
                                                         throw new Exception("Has to be DownloadNewChapters Job");
                            _parent.jobBoss.AddJob(new UpdateMetadata(this, dncJob.mangaConnector, dncJob.manga));
                        }

                        SendResponse(HttpStatusCode.Accepted, response);
                    }

                    break;
                case "Jobs/StartNow":
                    jobId = GetRequestVariable<string>(requestVariables, "jobId");
                    if (!_parent.jobBoss.TryGetJobById(jobId, out job))
                    {
                        SendResponse(HttpStatusCode.NotFound, response, $"jobId {jobId} not found.");
                        break;
                    }

                    _parent.jobBoss.AddJobToQueue(job!);
                    SendResponse(HttpStatusCode.Accepted, response);
                    break;
                case "Jobs/Cancel":
                    jobId = GetRequestVariable<string>(requestVariables, "jobId");
                    if (!_parent.jobBoss.TryGetJobById(jobId, out job))
                    {
                        SendResponse(HttpStatusCode.NotFound, response, $"jobId {jobId} not found.");
                        break;
                    }

                    job!.Cancel();
                    SendResponse(HttpStatusCode.Accepted, response);
                    break;
                case "Settings/UpdateDownloadLocation":
                    string downloadLocation = GetRequestVariable<string>(requestVariables, "downloadLocation");
                    bool moveFiles = GetRequestVariable<bool>(requestVariables, "moveFiles");
                    settings.UpdateDownloadLocation(downloadLocation, moveFiles);
                    SendResponse(HttpStatusCode.Accepted, response);
                    break;
                case "Settings/AprilFoolsMode":
                    bool aprilFoolsModeEnabled = GetRequestVariable<bool>(requestVariables, "enabled");
                    settings.UpdateAprilFoolsMode(aprilFoolsModeEnabled);
                    SendResponse(HttpStatusCode.Accepted, response);
                    break;
                case "Settings/userAgent":
                    string customUserAgent = GetRequestVariable<string>(requestVariables, "userAgent");
                    settings.UpdateUserAgent(customUserAgent);
                    SendResponse(HttpStatusCode.Accepted, response);
                    break;
                case "Settings/userAgent/Reset":
                    settings.UpdateUserAgent(null);
                    SendResponse(HttpStatusCode.Accepted, response);
                    break;
                case "Settings/customRequestLimit":
                    string requestTypeStr = GetRequestVariable<string>(requestVariables, "requestType");
                    int requestsPerMinute = GetRequestVariable<int>(requestVariables, "requestsPerMinute");
                    if (!Enum.TryParse(requestTypeStr, out RequestType requestType))
                    {
                        SendResponse(HttpStatusCode.BadRequest, response, $"requestType {requestTypeStr} does not exist.");
                        break;
                    }

                    if (settings.requestLimits.ContainsKey(requestType))
                    {
                        settings.requestLimits[requestType] = requestsPerMinute;
                        SendResponse(HttpStatusCode.Accepted, response);
                    }
                    else
                        SendResponse(HttpStatusCode.BadRequest, response, $"requestType {requestTypeStr} can not be configured.");

                    settings.ExportSettings();
                    break;
                case "Settings/customRequestLimit/Reset":
                    settings.requestLimits = TrangaSettings.DefaultRequestLimits;
                    settings.ExportSettings();
                    break;
                case "NotificationConnectors/Update":
                    notificationConnectorStr = GetRequestVariable<string>(requestVariables, "notificationConnector");
                    if (!Enum.TryParse(notificationConnectorStr, out notificationConnectorType))
                    {
                        SendResponse(HttpStatusCode.BadRequest, response, $"notificationConnector {notificationConnectorStr} does not exist.");
                        break;
                    }

                    if (notificationConnectorType is NotificationConnector.NotificationConnectorType.Gotify)
                    {
                        string gotifyUrl = GetRequestVariable<string>(requestVariables, "gotifyUrl");
                        string gotifyAppToken = GetRequestVariable<string>(requestVariables, "gotifyAppToken");
                        AddNotificationConnector(new Gotify(this, gotifyUrl, gotifyAppToken));
                        SendResponse(HttpStatusCode.Accepted, response);
                    }
                    else if (notificationConnectorType is NotificationConnector.NotificationConnectorType.LunaSea)
                    {
                        string lunaseaWebhook = GetRequestVariable<string>(requestVariables, "lunaseaWebhook");
                        AddNotificationConnector(new LunaSea(this, lunaseaWebhook));
                        SendResponse(HttpStatusCode.Accepted, response);
                    }
                    else if (notificationConnectorType is NotificationConnector.NotificationConnectorType.Ntfy)
                    {
                        string ntfyUrl = GetRequestVariable<string>(requestVariables, "ntfyUrl");
                        string ntfyAuth = GetRequestVariable<string>(requestVariables, "ntfyAuth");
                        AddNotificationConnector(new Ntfy(this, ntfyUrl, ntfyAuth));
                        SendResponse(HttpStatusCode.Accepted, response);
                    }
                    else
                    {
                        SendResponse(HttpStatusCode.BadRequest, response, $"notificationConnector {notificationConnectorType} can not be configured.");
                    }

                    break;
                case "NotificationConnectors/Test":
                    NotificationConnector notificationConnector;
                    notificationConnectorStr = GetRequestVariable<string>(requestVariables, "notificationConnector");
                    if (!Enum.TryParse(notificationConnectorStr, out notificationConnectorType))
                    {
                        SendResponse(HttpStatusCode.BadRequest, response, $"notificationConnector {notificationConnectorType} does not exist.");
                        break;
                    }

                    if (notificationConnectorType is NotificationConnector.NotificationConnectorType.Gotify)
                    {
                        string gotifyUrl = GetRequestVariable<string>(requestVariables, "gotifyUrl");
                        string gotifyAppToken = GetRequestVariable<string>(requestVariables, "gotifyAppToken");
                        notificationConnector = new Gotify(this, gotifyUrl, gotifyAppToken);
                    }
                    else if (notificationConnectorType is NotificationConnector.NotificationConnectorType.LunaSea)
                    {
                        string lunaseaWebhook = GetRequestVariable<string>(requestVariables, "lunaseaWebhook");
                        notificationConnector = new LunaSea(this, lunaseaWebhook);
                    }
                    else if (notificationConnectorType is NotificationConnector.NotificationConnectorType.Ntfy)
                    {
                        string ntfyUrl = GetRequestVariable<string>(requestVariables, "ntfyUrl");
                        string ntfyAuth = GetRequestVariable<string>(requestVariables, "ntfyAuth");
                        notificationConnector = new Ntfy(this, ntfyUrl, ntfyAuth);
                    }
                    else
                    {
                        SendResponse(HttpStatusCode.BadRequest, response, $"notificationConnector {notificationConnectorType} can not be tested.");
                        break;
                    }

                    notificationConnector.SendNotification("Tranga Test", "This is Test-Notification.");
                    SendResponse(HttpStatusCode.Accepted, response);
                    break;
                case "NotificationConnectors/Reset":
                    notificationConnectorStr = GetRequestVariable<string>(requestVariables, "notificationConnector");
                    if (!Enum.TryParse(notificationConnectorStr, out notificationConnectorType))
                    {
                        SendResponse(HttpStatusCode.BadRequest, response, $"notificationConnector {notificationConnectorType} does not exist.");
                        break;
                    }

                    DeleteNotificationConnector(notificationConnectorType);
                    SendResponse(HttpStatusCode.Accepted, response);
                    break;
                case "LibraryConnectors/Update":
                    libraryConnectorStr = GetRequestVariable<string>(requestVariables, "libraryConnector");
                    if (!Enum.TryParse(libraryConnectorStr, out libraryConnectorType))
                    {
                        SendResponse(HttpStatusCode.BadRequest, response, $"libraryConnector {libraryConnectorStr} does not exist.");
                        break;
                    }

                    if (libraryConnectorType is LibraryConnector.LibraryType.Kavita)
                    {
                        string kavitaUrl = GetRequestVariable<string>(requestVariables, "kavitaUrl");
                        string kavitaUsername = GetRequestVariable<string>(requestVariables, "kavitaUsername");
                        string kavitaPassword = GetRequestVariable<string>(requestVariables, "kavitaPassword");
                        AddLibraryConnector(new Kavita(this, kavitaUrl, kavitaUsername, kavitaPassword));
                        SendResponse(HttpStatusCode.Accepted, response);
                    }
                    else if (libraryConnectorType is LibraryConnector.LibraryType.Komga)
                    {
                        string komgaUrl = GetRequestVariable<string>(requestVariables, "komgaUrl");
                        string komgaAuth = GetRequestVariable<string>(requestVariables, "komgaAuth");
                        AddLibraryConnector(new Komga(this, komgaUrl, komgaAuth));
                        SendResponse(HttpStatusCode.Accepted, response);
                    }
                    else
                    {
                        SendResponse(HttpStatusCode.BadRequest, response, $"libraryConnector {libraryConnectorStr} can not be configured.");
                    }

                    break;
                case "LibraryConnectors/Test":
                    LibraryConnector libraryConnector;
                    libraryConnectorStr = GetRequestVariable<string>(requestVariables, "libraryConnector");
                    if (!Enum.TryParse(libraryConnectorStr, out libraryConnectorType))
                    {
                        SendResponse(HttpStatusCode.BadRequest, response, $"libraryConnector {libraryConnectorStr} does not exist.");
                        break;
                    }

                    if (libraryConnectorType is LibraryConnector.LibraryType.Kavita)
                    {
                        string kavitaUrl = GetRequestVariable<string>(requestVariables, "kavitaUrl");
                        string kavitaUsername = GetRequestVariable<string>(requestVariables, "kavitaUsername");
                        string kavitaPassword = GetRequestVariable<string>(requestVariables, "kavitaPassword");
                        libraryConnector = new Kavita(this, kavitaUrl, kavitaUsername, kavitaPassword);
                    }
                    else if (libraryConnectorType is LibraryConnector.LibraryType.Komga)
                    {
                        string komgaUrl = GetRequestVariable<string>(requestVariables, "komgaUrl");
                        string komgaAuth = GetRequestVariable<string>(requestVariables, "komgaAuth");
                        libraryConnector = new Komga(this, komgaUrl, komgaAuth);
                    }
                    else
                    {
                        SendResponse(HttpStatusCode.BadRequest, response, $"libraryConnector {libraryConnectorStr} can not be tested.");
                        break;
                    }

                    libraryConnector.UpdateLibrary();
                    SendResponse(HttpStatusCode.Accepted, response);
                    break;
                case "LibraryConnectors/Reset":
                    libraryConnectorStr = GetRequestVariable<string>(requestVariables, "libraryConnector");
                    if (!Enum.TryParse(libraryConnectorStr, out libraryConnectorType))
                    {
                        SendResponse(HttpStatusCode.BadRequest, response, $"libraryConnector {libraryConnectorStr} does not exist.");
                        break;
                    }

                    DeleteLibraryConnector(libraryConnectorType);
                    SendResponse(HttpStatusCode.Accepted, response);
                    break;
                default:
                    SendResponse(HttpStatusCode.NotFound, response, "Request-URI does not exist.");
                    break;
            }
        }
        catch (Exception e)
        {
            SendResponse(HttpStatusCode.BadRequest, response, e.Message);
        }
    }

    private void HandleDelete(HttpListenerRequest request, HttpListenerResponse response)
    {
        Dictionary<string, JsonElement> requestVariables = GetRequestVariables(request);
        string path = Regex.Match(request.Url!.LocalPath, @"[A-z0-9]+(\/[A-z0-9]+)*").Value;
        try
        {
            switch (path)
            {
                case "Jobs":
                    string jobId = GetRequestVariable<string>(requestVariables, "jobId");
                    if (!_parent.jobBoss.TryGetJobById(jobId, out Job? job))
                    {
                        SendResponse(HttpStatusCode.NotFound, response, $"jobId {jobId} not found.");
                        break;
                    }

                    _parent.jobBoss.RemoveJob(job!);
                    SendResponse(HttpStatusCode.Accepted, response);
                    break;
                case "Jobs/DownloadNewChapters":
                    string connectorName = GetRequestVariable<string>(requestVariables, "connector");
                    string internalId = GetRequestVariable<string>(requestVariables, "internalId");
                    if (!_parent.TryGetConnector(connectorName, out MangaConnector? connector))
                    {
                        SendResponse(HttpStatusCode.BadRequest, response, $"connector {connectorName} does not exist.");
                        break;
                    }
                    if (!_parent.TryGetPublicationById(internalId, out var manga))
                    {
                        SendResponse(HttpStatusCode.NotFound, response, $"internalId {internalId} not found.");
                        break;
                    }
                    _parent.jobBoss.RemoveJobs(_parent.jobBoss.GetJobsLike(connector, manga));
                    SendResponse(HttpStatusCode.Accepted, response);
                    break;
                case "NotificationConnectors":
                    string notificationConnectorStr =
                        GetRequestVariable<string>(requestVariables, "notificationConnector");
                    if (!Enum.TryParse(notificationConnectorStr,
                            out NotificationConnector.NotificationConnectorType notificationConnectorType))
                    {
                        SendResponse(HttpStatusCode.BadRequest, response, $"notificationConnector {notificationConnectorType} does not exist.");
                        break;
                    }

                    DeleteNotificationConnector(notificationConnectorType);
                    SendResponse(HttpStatusCode.Accepted, response);
                    break;
                case "LibraryConnectors":
                    string libraryConnectorStr = GetRequestVariable<string>(requestVariables, "libraryConnector");
                    if (!Enum.TryParse(libraryConnectorStr, out LibraryConnector.LibraryType libraryConnectorType))
                    {
                        SendResponse(HttpStatusCode.BadRequest, response, $"libraryConnector {libraryConnectorStr} does not exist.");
                        break;
                    }

                    DeleteLibraryConnector(libraryConnectorType);
                    SendResponse(HttpStatusCode.Accepted, response);
                    break;
                default:
                    SendResponse(HttpStatusCode.NotFound, response, "Request-URI does not exist.");
                    break;
            }
        }
        catch (Exception e)
        {
            SendResponse(HttpStatusCode.BadRequest, response, e.Message);
        }
    }

    private void SendResponse(HttpStatusCode statusCode, HttpListenerResponse response, object? content = null)
    {
        //Log($"Response: {statusCode} {content}");
        response.StatusCode = (int)statusCode;
        response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
        response.AddHeader("Access-Control-Allow-Methods", "GET, POST, DELETE");
        response.AddHeader("Access-Control-Max-Age", "1728000");
        response.AppendHeader("Access-Control-Allow-Origin", "*");

        if (content is not Stream)
        {
            response.ContentType = "application/json";
            try
            {
                response.OutputStream.Write(content is not null
                    ? Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(content))
                    : Array.Empty<byte>());
                response.OutputStream.Close();
            }
            catch (HttpListenerException e)
            {
                Log(e.ToString());
            }
        }
        else if(content is FileStream stream)
        {
            string contentType = stream.Name.Split('.')[^1];
            switch (contentType.ToLower())
            {
                case "gif":
                    response.ContentType = "image/gif";
                    break;
                case "png":
                    response.ContentType = "image/png";
                    break;
                case "jpg":
                case "jpeg":
                    response.ContentType = "image/jpeg";
                    break;
                case "log":
                    response.ContentType = "text/plain";
                    break;
            }
            stream.CopyTo(response.OutputStream);
            response.OutputStream.Close();
            stream.Close();
        }
    }
}