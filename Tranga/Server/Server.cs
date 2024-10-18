using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Tranga.Server;

public partial class Server : GlobalBase, IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly Tranga _parent;
    private bool _running = true;

    private readonly List<RequestPath> _apiRequestPaths;

    public Server(Tranga parent) : base(parent)
    {
        /*
         * Contains all valid Request Methods, Paths (with Regex Group Matching for specific Parameters) and Handling Methods
         */
        _apiRequestPaths = new List<RequestPath>
        {
            new ("GET", @"/v2/Connector/Types", GetV2ConnectorTypes),
            new ("GET", @"/v2/Connector/([a-zA-Z]+)/GetManga", GetV2ConnectorConnectorNameGetManga),
            new ("GET", @"/v2/Mangas", GetV2Mangas),
            new ("GET", @"/v2/Manga/Search", GetV2MangaSearch),
            new ("GET", @"/v2/Manga", GetV2Manga),
            new ("GET", @"/v2/Manga/([-A-Za-z0-9]*={0,3})", GetV2MangaInternalId),
            new ("DELETE", @"/v2/Manga/([-A-Za-z0-9]*={0,3})", DeleteV2MangaInternalId),
            new ("GET", @"/v2/Manga/([-A-Za-z0-9]*={0,3})/Cover", GetV2MangaInternalIdCover),
            new ("GET", @"/v2/Manga/([-A-Za-z0-9]*={0,3})/Chapters", GetV2MangaInternalIdChapters),
            new ("GET", @"/v2/Manga/([-A-Za-z0-9]*={0,3})/Chapters/Latest", GetV2MangaInternalIdChaptersLatest),
            new ("POST", @"/v2/Manga/([-A-Za-z0-9]*={0,3})/ignoreChaptersBelow", PostV2MangaInternalIdIgnoreChaptersBelow),
            new ("POST", @"/v2/Manga/([-A-Za-z0-9]*={0,3})/moveFolder", PostV2MangaInternalIdMoveFolder),
            new ("GET", @"/v2/Jobs", GetV2Jobs),
            new ("GET", @"/v2/Jobs/Running", GetV2JobsRunning),
            new ("GET", @"/v2/Jobs/Waiting", GetV2JobsWaiting),
            new ("GET", @"/v2/Jobs/Monitoring", GetV2JobsMonitoring),
            new ("GET", @"/v2/Job/Types", GetV2JobTypes),
            new ("POST", @"/v2/Job/Create/([a-zA-Z]+)", PostV2JobCreateType),
            new ("GET", @"/v2/Job", GetV2Job),
            new ("GET", @"/v2/Job/([a-zA-Z\.]+-[-A-Za-z0-9+/]*={0,3}(?:-[0-9]+)?)/Progress", GetV2JobJobIdProgress),
            new ("POST", @"/v2/Job/([a-zA-Z\.]+-[-A-Za-z0-9+/]*={0,3}(?:-[0-9]+)?)/StartNow", PostV2JobJobIdStartNow),
            new ("POST", @"/v2/Job/([a-zA-Z\.]+-[-A-Za-z0-9+/]*={0,3}(?:-[0-9]+)?)/Cancel", PostV2JobJobIdCancel),
            new ("GET", @"/v2/Job/([a-zA-Z\.]+-[-A-Za-z0-9+/]*={0,3}(?:-[0-9]+)?)", GetV2JobJobId),
            new ("DELETE", @"/v2/Job/([a-zA-Z\.]+-[-A-Za-z0-9+/]*={0,3}(?:-[0-9]+)?)", DeleteV2JobJobId),
            new ("GET", @"/v2/Settings", GetV2Settings),
            new ("GET", @"/v2/Settings/UserAgent", GetV2SettingsUserAgent),
            new ("POST", @"/v2/Settings/UserAgent", PostV2SettingsUserAgent),
            new ("GET", @"/v2/Settings/RateLimit/Types", GetV2SettingsRateLimitTypes),
            new ("GET", @"/v2/Settings/RateLimit", GetV2SettingsRateLimit),
            new ("POST", @"/v2/Settings/RateLimit", PostV2SettingsRateLimit),
            new ("GET", @"/v2/Settings/RateLimit/([a-zA-Z]+)", GetV2SettingsRateLimitType),
            new ("POST", @"/v2/Settings/RateLimit/([a-zA-Z]+)", PostV2SettingsRateLimitType),
            new ("GET", @"/v2/Settings/AprilFoolsMode", GetV2SettingsAprilFoolsMode),
            new ("POST", @"/v2/Settings/AprilFoolsMode", PostV2SettingsAprilFoolsMode),
            new ("POST", @"/v2/Settings/DownloadLocation", PostV2SettingsDownloadLocation),
            new ("GET", @"/v2/LibraryConnector", GetV2LibraryConnector),
            new ("GET", @"/v2/LibraryConnector/Types", GetV2LibraryConnectorTypes),
            new ("GET", @"/v2/LibraryConnector/([a-zA-Z]+)", GetV2LibraryConnectorType),
            new ("POST", @"/v2/LibraryConnector/([a-zA-Z]+)", PostV2LibraryConnectorType),
            new ("POST", @"/v2/LibraryConnector/([a-zA-Z]+)/Test", PostV2LibraryConnectorTypeTest),
            new ("DELETE", @"/v2/LibraryConnector/([a-zA-Z]+)", DeleteV2LibraryConnectorType),
            new ("GET", @"/v2/NotificationConnector", GetV2NotificationConnector),
            new ("GET", @"/v2/NotificationConnector/Types", GetV2NotificationConnectorTypes),
            new ("GET", @"/v2/NotificationConnector/([a-zA-Z]+)", GetV2NotificationConnectorType),
            new ("POST", @"/v2/NotificationConnector/([a-zA-Z]+)", PostV2NotificationConnectorType),
            new ("POST", @"/v2/NotificationConnector/([a-zA-Z]+)/Test", PostV2NotificationConnectorTypeTest),
            new ("DELETE", @"/v2/NotificationConnector/([a-zA-Z]+)", DeleteV2NotificationConnectorType),
            new ("GET", @"/v2/LogFile", GetV2LogFile),
            new ("GET", @"/v2/Ping", GetV2Ping),
            new ("POST", @"/v2/Ping", PostV2Ping)
        };
        
        this._parent = parent;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            this._listener.Prefixes.Add($"http://*:{TrangaSettings.apiPortNumber}/");
        else
            this._listener.Prefixes.Add($"http://localhost:{TrangaSettings.apiPortNumber}/");
        Thread listenThread = new(Listen);
        listenThread.Start();
        while(_parent.keepRunning && _running)
            Thread.Sleep(100);
        this.Dispose();
    }

    private void Listen()
    {
        this._listener.Start();
        foreach (string prefix in this._listener.Prefixes)
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
        if (request.HttpMethod == "OPTIONS")
        {
            SendResponse(HttpStatusCode.NoContent, response);//Response always contains all valid Request-Methods
            return;
        } 
        if (request.Url!.LocalPath.Contains("favicon"))
        {
            SendResponse(HttpStatusCode.NoContent, response);
            return;
        }
        string path = Regex.Match(request.Url.LocalPath, @"\/[a-zA-Z0-9\.+/=-]+(\/[a-zA-Z0-9\.+/=-]+)*").Value; //Local Path

        if (!Regex.IsMatch(path, "/v2(/.*)?")) //Use only v2 API
        {
            SendResponse(HttpStatusCode.NotFound, response, "Use Version 2 API");
            return;
        }
        
        Dictionary<string, string> requestVariables = GetRequestVariables(request.Url!.Query);  //Variables in the URI
        Dictionary<string, string> requestBody = GetRequestBody(request);                       //Variables in the JSON body
        Dictionary<string, string> requestParams = requestVariables.UnionBy(requestBody, v => v.Key)
            .ToDictionary(kv => kv.Key, kv => kv.Value); //The actual variable used for the API

        ValueTuple<HttpStatusCode, object?> responseMessage; //Used to respond to the HttpRequest
        if (_apiRequestPaths.Any(p => p.HttpMethod == request.HttpMethod && Regex.Match(path, p.RegexStr).Length == path.Length)) //Check if Request-Path is valid
        {
            RequestPath requestPath =
                _apiRequestPaths.First(p => p.HttpMethod == request.HttpMethod && Regex.Match(path, p.RegexStr).Length == path.Length);
            responseMessage =
                requestPath.Method.Invoke(Regex.Match(path, requestPath.RegexStr).Groups, requestParams); //Get HttpResponse content
        }
        else
            responseMessage = new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.MethodNotAllowed, "Unknown Request Path");
        
        SendResponse(responseMessage.Item1, response, responseMessage.Item2);
    }

    private Dictionary<string, string> GetRequestVariables(string query)
    {
        Dictionary<string, string> ret = new();
        Regex queryRex = new(@"\?{1}&?([A-z0-9-=]+=[A-z0-9-=]+)+(&[A-z0-9-=]+=[A-z0-9-=]+)*");
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

    private Dictionary<string, string> GetRequestBody(HttpListenerRequest request)
    {
        if (!request.HasEntityBody)
        {
            //Nospam Log("No request body");
            return new Dictionary<string, string>();
        }
        Stream body = request.InputStream;
        Encoding encoding = request.ContentEncoding;
        using StreamReader streamReader = new (body, encoding);
        try
        {
            Dictionary<string, string> requestBody =
                JsonConvert.DeserializeObject<Dictionary<string, string>>(streamReader.ReadToEnd())
                ?? new();
            return requestBody;
        }
        catch (JsonException e)
        {
            Log(e.Message);
        }
        return new Dictionary<string, string>();
    }
    
    private void SendResponse(HttpStatusCode statusCode, HttpListenerResponse response, object? content = null)
    {
        //Log($"Response: {statusCode} {content}");
        response.StatusCode = (int)statusCode;
        response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
        response.AddHeader("Access-Control-Allow-Methods", "GET, POST, DELETE");
        response.AddHeader("Access-Control-Max-Age", "1728000");
        response.AppendHeader("Access-Control-Allow-Origin", "*");


        try
        {
            if (content is Stream stream)
            {
                response.ContentType = "image/jpeg";
                response.AddHeader("Cache-Control", "max-age=600");
                stream.CopyTo(response.OutputStream);
                stream.Close();
            }
            else
            {
                response.ContentType = "application/json";
                response.AddHeader("Cache-Control", "no-store");
                response.OutputStream.Write(content is not null
                    ? Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(content))
                    : Array.Empty<byte>());
            }

            response.OutputStream.Close();
        }
        catch (HttpListenerException e)
        {
            Log(e.ToString());
        }
    }


    public void Dispose()
    {
        _running = false;
        ((IDisposable)_listener).Dispose();
    }
}