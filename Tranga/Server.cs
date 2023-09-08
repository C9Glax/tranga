using System.Net;
using System.Runtime.InteropServices;
using System.Text;
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
            catch (HttpListenerException e)
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
    
    private Dictionary<string, string> GetRequestVariables(string query)
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

    private void HandleGet(HttpListenerRequest request, HttpListenerResponse response)
    {
        Dictionary<string, string> requestVariables = GetRequestVariables(request.Url!.Query);
        string? connectorName, jobId, internalId;
        MangaConnector? connector;
        Manga? manga;
        string path = Regex.Match(request.Url!.LocalPath, @"[A-z0-9]+(\/[A-z0-9]+)*").Value;
        switch (path)
        {
            case "Connectors":
                SendResponse(HttpStatusCode.OK, response, _parent.GetConnectors().Select(con => con.name).ToArray());
                break;
            case "Manga/Cover":
                if (!requestVariables.TryGetValue("internalId", out internalId) ||
                    !_parent.TryGetPublicationById(internalId, out manga))
                {
                    SendResponse(HttpStatusCode.BadRequest, response);
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
                requestVariables.TryGetValue("title", out string? title);
                requestVariables.TryGetValue("url", out string? url);
                if (!requestVariables.TryGetValue("connector", out connectorName) ||
                    !_parent.TryGetConnector(connectorName, out connector) ||
                    (title is null && url is null))
                {
                    SendResponse(HttpStatusCode.BadRequest, response);
                    break;
                }

                if (url is not null)
                {
                    HashSet<Manga> ret = new();
                    manga = connector!.GetMangaFromUrl(url);
                    if (manga is not null)
                        ret.Add((Manga)manga);
                    SendResponse(HttpStatusCode.OK, response, ret);
                }else
                    SendResponse(HttpStatusCode.OK, response, connector!.GetManga(title!));
                break;
            case "Manga/Chapters":
                if(!requestVariables.TryGetValue("connector", out connectorName) ||
                   !requestVariables.TryGetValue("internalId", out internalId) ||
                   !_parent.TryGetConnector(connectorName, out connector) ||
                   !_parent.TryGetPublicationById(internalId, out manga))
                {
                    SendResponse(HttpStatusCode.BadRequest, response);
                    break;
                }
                SendResponse(HttpStatusCode.OK, response, connector!.GetChapters((Manga)manga!));
                break;
            case "Jobs":
                if (!requestVariables.TryGetValue("jobId", out jobId))
                {
                    if(!_parent.jobBoss.jobs.Any(jjob => jjob.id == jobId))
                        SendResponse(HttpStatusCode.BadRequest, response);
                    else
                        SendResponse(HttpStatusCode.OK, response, _parent.jobBoss.jobs.First(jjob => jjob.id == jobId));
                    break;
                }
                SendResponse(HttpStatusCode.OK, response, _parent.jobBoss.jobs);
                break;
            case "Jobs/Progress":
                if (!requestVariables.TryGetValue("jobId", out jobId))
                {
                    if(!_parent.jobBoss.jobs.Any(jjob => jjob.id == jobId))
                        SendResponse(HttpStatusCode.BadRequest, response);
                    else
                        SendResponse(HttpStatusCode.OK, response, _parent.jobBoss.jobs.First(jjob => jjob.id == jobId).progressToken);
                    break;
                }
                SendResponse(HttpStatusCode.OK, response, _parent.jobBoss.jobs.Select(jjob => jjob.progressToken));
                break;
            case "Jobs/Running":
                SendResponse(HttpStatusCode.OK, response, _parent.jobBoss.jobs.Where(jjob => jjob.progressToken.state is ProgressToken.State.Running));
                break;
            case "Jobs/Waiting":
                SendResponse(HttpStatusCode.OK, response, _parent.jobBoss.jobs.Where(jjob => jjob.progressToken.state is ProgressToken.State.Standby));
                break;
            case "Jobs/MonitorJobs":
                SendResponse(HttpStatusCode.OK, response, _parent.jobBoss.jobs.Where(jjob => jjob is DownloadNewChapters));
                break;
            case "Settings":
                SendResponse(HttpStatusCode.OK, response, settings);
                break;
            case "NotificationConnectors":
                SendResponse(HttpStatusCode.OK, response, notificationConnectors);
                break;
            case "NotificationConnectors/Types":
                SendResponse(HttpStatusCode.OK, response,
                    Enum.GetValues<NotificationConnector.NotificationConnectorType>().Select(nc => new KeyValuePair<byte, string?>((byte)nc, Enum.GetName(nc))));
                break;
            case "LibraryConnectors":
                SendResponse(HttpStatusCode.OK, response, libraryConnectors);
                break;
            case "LibraryConnectors/Types":
                SendResponse(HttpStatusCode.OK, response, 
                    Enum.GetValues<LibraryConnector.LibraryType>().Select(lc => new KeyValuePair<byte, string?>((byte)lc, Enum.GetName(lc))));
                break;
            case "Ping":
                SendResponse(HttpStatusCode.OK, response, "Pong");
                break;
            default:
                SendResponse(HttpStatusCode.BadRequest, response);
                break;
        }
    }

    private void HandlePost(HttpListenerRequest request, HttpListenerResponse response)
    {
        Dictionary<string, string> requestVariables = GetRequestVariables(request.Url!.Query);
        string? connectorName, internalId, jobId, chapterNumStr, customFolderName;
        MangaConnector? connector;
        Manga? tmpManga;
        Manga manga;
        Job? job;
        string path = Regex.Match(request.Url!.LocalPath, @"[A-z0-9]+(\/[A-z0-9]+)*").Value;
        switch (path)
        {
            case "Manga":
                if(!requestVariables.TryGetValue("internalId", out internalId) ||
                   !_parent.TryGetPublicationById(internalId, out tmpManga))
                {
                    SendResponse(HttpStatusCode.BadRequest, response);
                    break;
                }
                manga = (Manga)tmpManga!;
                SendResponse(HttpStatusCode.OK, response, manga);
                break;
            case "Jobs/MonitorManga":
                if(!requestVariables.TryGetValue("connector", out connectorName) ||
                   !requestVariables.TryGetValue("internalId", out internalId) ||
                   !requestVariables.TryGetValue("interval", out string? intervalStr) ||
                   !_parent.TryGetConnector(connectorName, out connector)||
                   !_parent.TryGetPublicationById(internalId, out tmpManga) ||
                   !TimeSpan.TryParse(intervalStr, out TimeSpan interval))
                {
                    SendResponse(HttpStatusCode.BadRequest, response);
                    break;
                }

                manga = (Manga)tmpManga!;
                
                if (requestVariables.TryGetValue("ignoreBelowChapterNum", out chapterNumStr))
                {
                    if (!float.TryParse(chapterNumStr, numberFormatDecimalPoint, out float chapterNum))
                    {
                        SendResponse(HttpStatusCode.BadRequest, response);
                        break;
                    }
                    manga.ignoreChaptersBelow = chapterNum;
                }
                
                if (requestVariables.TryGetValue("customFolderName", out customFolderName))
                    manga.MovePublicationFolder(settings.downloadLocation, customFolderName);
                
                _parent.jobBoss.AddJob(new DownloadNewChapters(this, connector!, manga, true, interval));
                SendResponse(HttpStatusCode.Accepted, response);
                break;
            case "Jobs/DownloadNewChapters":
                if(!requestVariables.TryGetValue("connector", out connectorName) ||
                   !requestVariables.TryGetValue("internalId", out internalId) ||
                   !_parent.TryGetConnector(connectorName, out connector)||
                   !_parent.TryGetPublicationById(internalId, out tmpManga))
                {
                    SendResponse(HttpStatusCode.BadRequest, response);
                    break;
                }

                manga = (Manga)tmpManga!;
                
                if (requestVariables.TryGetValue("ignoreBelowChapterNum", out chapterNumStr))
                {
                    if (!float.TryParse(chapterNumStr, numberFormatDecimalPoint, out float chapterNum))
                    {
                        SendResponse(HttpStatusCode.BadRequest, response);
                        break;
                    }
                    manga.ignoreChaptersBelow = chapterNum;
                }

                if (requestVariables.TryGetValue("customFolderName", out customFolderName))
                    manga.MovePublicationFolder(settings.downloadLocation, customFolderName);
                
                _parent.jobBoss.AddJob(new DownloadNewChapters(this, connector!, manga, false));
                SendResponse(HttpStatusCode.Accepted, response);
                break;
            case "Jobs/StartNow":
                if (!requestVariables.TryGetValue("jobId", out jobId) ||
                    !_parent.jobBoss.TryGetJobById(jobId, out job))
                {
                    SendResponse(HttpStatusCode.BadRequest, response);
                    break;
                }
                _parent.jobBoss.AddJobToQueue(job!);
                SendResponse(HttpStatusCode.Accepted, response);
                break;
            case "Jobs/Cancel":
                if (!requestVariables.TryGetValue("jobId", out jobId) ||
                    !_parent.jobBoss.TryGetJobById(jobId, out job))
                {
                    SendResponse(HttpStatusCode.BadRequest, response);
                    break;
                }
                job!.Cancel();
                SendResponse(HttpStatusCode.Accepted, response);
                break;
            case "Settings/UpdateDownloadLocation":
                if (!requestVariables.TryGetValue("downloadLocation", out string? downloadLocation) ||
                    !requestVariables.TryGetValue("moveFiles", out string? moveFilesStr) ||
                    !Boolean.TryParse(moveFilesStr, out bool moveFiles))
                {
                    SendResponse(HttpStatusCode.BadRequest, response);
                    break;
                }
                settings.UpdateDownloadLocation(downloadLocation, moveFiles);
                SendResponse(HttpStatusCode.Accepted, response);
                break;
            /*case "Settings/UpdateWorkingDirectory":
                if (!requestVariables.TryGetValue("workingDirectory", out string? workingDirectory))
                {
                    SendResponse(HttpStatusCode.BadRequest, response);
                    break;
                }
                settings.UpdateWorkingDirectory(workingDirectory);
                SendResponse(HttpStatusCode.Accepted, response);
                break;*/
            case "NotificationConnectors/Update":
                if (!requestVariables.TryGetValue("notificationConnector", out string? notificationConnectorStr) ||
                    !Enum.TryParse(notificationConnectorStr, out NotificationConnector.NotificationConnectorType notificationConnectorType))
                {
                    SendResponse(HttpStatusCode.BadRequest, response);
                    break;
                }

                if (notificationConnectorType is NotificationConnector.NotificationConnectorType.Gotify)
                {
                    if (!requestVariables.TryGetValue("gotifyUrl", out string? gotifyUrl) ||
                        !requestVariables.TryGetValue("gotifyAppToken", out string? gotifyAppToken))
                    {
                        SendResponse(HttpStatusCode.BadRequest, response);
                        break;
                    }
                    AddNotificationConnector(new Gotify(this, gotifyUrl, gotifyAppToken));
                    SendResponse(HttpStatusCode.Accepted, response);
                    break;
                }

                if (notificationConnectorType is NotificationConnector.NotificationConnectorType.LunaSea)
                {
                    if (!requestVariables.TryGetValue("lunaseaWebhook", out string? lunaseaWebhook))
                    {
                        SendResponse(HttpStatusCode.BadRequest, response);
                        break;
                    }
                    AddNotificationConnector(new LunaSea(this, lunaseaWebhook));
                    SendResponse(HttpStatusCode.Accepted, response);
                    break;
                }
                break;
            case "LibraryConnectors/Update":
                if (!requestVariables.TryGetValue("libraryConnector", out string? libraryConnectorStr) ||
                    !Enum.TryParse(libraryConnectorStr,
                        out LibraryConnector.LibraryType libraryConnectorType))
                {
                    SendResponse(HttpStatusCode.BadRequest, response);
                    break;
                }

                if (libraryConnectorType is LibraryConnector.LibraryType.Kavita)
                {
                    if (!requestVariables.TryGetValue("kavitaUrl", out string? kavitaUrl) ||
                        !requestVariables.TryGetValue("kavitaUsername", out string? kavitaUsername) ||
                        !requestVariables.TryGetValue("kavitaPassword", out string? kavitaPassword))
                    {
                        SendResponse(HttpStatusCode.BadRequest, response);
                        break;
                    }
                    AddLibraryConnector(new Kavita(this, kavitaUrl, kavitaUsername, kavitaPassword));
                    SendResponse(HttpStatusCode.Accepted, response);
                    break;
                }

                if (libraryConnectorType is LibraryConnector.LibraryType.Komga)
                {
                    if (!requestVariables.TryGetValue("komgaUrl", out string? komgaUrl) ||
                        !requestVariables.TryGetValue("komgaAuth", out string? komgaAuth))
                    {
                        SendResponse(HttpStatusCode.BadRequest, response);
                        break;
                    }
                    AddLibraryConnector(new Komga(this, komgaUrl, komgaAuth));
                    SendResponse(HttpStatusCode.Accepted, response);
                    break;
                }
                break;
            case "LogMessages":
                if (logger is null || !File.Exists(logger?.logFilePath))
                {
                    SendResponse(HttpStatusCode.NotFound, response);
                    break;
                }
                SendResponse(HttpStatusCode.OK, response, logger.GetLog());
                break;
            case "LogFile":
                if (logger is null || !File.Exists(logger?.logFilePath))
                {
                    SendResponse(HttpStatusCode.NotFound, response);
                    break;
                }

                string logDir = new FileInfo(logger.logFilePath).DirectoryName!;
                string tmpFilePath = Path.Join(logDir, "Tranga.log");
                File.Copy(logger.logFilePath, tmpFilePath);
                SendResponse(HttpStatusCode.OK, response, new FileStream(tmpFilePath, FileMode.Open));
                File.Delete(tmpFilePath);
                break;
            default:
                SendResponse(HttpStatusCode.BadRequest, response);
                break;
        }
    }

    private void HandleDelete(HttpListenerRequest request, HttpListenerResponse response)
    {
        Dictionary<string, string> requestVariables = GetRequestVariables(request.Url!.Query);
        string? connectorName, internalId;
        MangaConnector connector;
        Manga manga;
        string path = Regex.Match(request.Url!.LocalPath, @"[A-z0-9]+(\/[A-z0-9]+)*").Value;
        switch (path)
        {
            case "Jobs":
                if (!requestVariables.TryGetValue("jobId", out string? jobId) ||
                    !_parent.jobBoss.TryGetJobById(jobId, out Job? job))
                {
                    SendResponse(HttpStatusCode.BadRequest, response);
                    break;
                }
                _parent.jobBoss.RemoveJob(job!);
                SendResponse(HttpStatusCode.Accepted, response);
                break;
            case "Jobs/DownloadNewChapters":
                if(!requestVariables.TryGetValue("connector", out connectorName) ||
                   !requestVariables.TryGetValue("internalId", out internalId) ||
                   _parent.GetConnector(connectorName) is null ||
                   _parent.GetPublicationById(internalId) is null)
                {
                    SendResponse(HttpStatusCode.BadRequest, response);
                    break;
                }
                connector = _parent.GetConnector(connectorName)!;
                manga = (Manga)_parent.GetPublicationById(internalId)!;
                _parent.jobBoss.RemoveJobs(_parent.jobBoss.GetJobsLike(connector, manga));
                SendResponse(HttpStatusCode.Accepted, response);
                break;
            case "NotificationConnectors":
                if (!requestVariables.TryGetValue("notificationConnector", out string? notificationConnectorStr) ||
                    !Enum.TryParse(notificationConnectorStr, out NotificationConnector.NotificationConnectorType notificationConnectorType))
                {
                    SendResponse(HttpStatusCode.BadRequest, response);
                    break;
                }
                DeleteNotificationConnector(notificationConnectorType);
                SendResponse(HttpStatusCode.Accepted, response);
                break;
            case "LibraryConnectors":
                if (!requestVariables.TryGetValue("libraryConnectors", out string? libraryConnectorStr) ||
                    !Enum.TryParse(libraryConnectorStr,
                        out LibraryConnector.LibraryType libraryConnectoryType))
                {
                    SendResponse(HttpStatusCode.BadRequest, response);
                    break;
                }
                DeleteLibraryConnector(libraryConnectoryType);
                SendResponse(HttpStatusCode.Accepted, response);
                break;
            default:
                SendResponse(HttpStatusCode.BadRequest, response);
                break;
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

        if (content is not FileStream stream)
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
        else
        {
            stream.CopyTo(response.OutputStream);
            response.OutputStream.Close();
            stream.Close();
        }
    }
}