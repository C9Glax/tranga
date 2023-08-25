using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Tranga.MangaConnectors;

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
        Thread t = new (Listen);
        t.Start();
    }

    private void Listen()
    {
        this._listener.Start();
        foreach(string prefix in this._listener.Prefixes)
            Log($"Listening on {prefix}");
        while (this._listener.IsListening && _parent.keepRunning)
        {
            HttpListenerContext context = this._listener.GetContextAsync().Result;
            Log($"{context.Request.HttpMethod} {context.Request.Url} {context.Request.UserAgent}");
            Task t = new(() =>
            {
                HandleRequest(context);
            });
            t.Start();
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
                HandleGet(request, request.InputStream, response);
                break;
            case "POST":
                HandlePost(request, request.InputStream, response);
                break;
            case "DELETE":
                HandleDelete(request, request.InputStream, response);
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
        foreach (string kvpair in query.Split('&').Where(str => str.Length >= 3))
        {
            string var = kvpair.Split('=')[0];
            string val = Regex.Replace(kvpair.Substring(var.Length + 1), "%20", " ");
            val = Regex.Replace(val, "%[0-9]{2}", "");
            ret.Add(var, val);
        }
        return ret;
    }

    private void HandleGet(HttpListenerRequest request, Stream content, HttpListenerResponse response)
    {
        Dictionary<string, string> requestVariables = GetRequestVariables(request.Url!.Query);
        switch (request.Url!.LocalPath[1..])
        {
            case "Connectors":
                SendResponse(HttpStatusCode.OK, response, _parent.GetConnectors().Select(connector => connector.name).ToArray());
                break;
            case "Publications/FromConnector":
                if (requestVariables.TryGetValue("connector", out string? connectorName) &&
                    requestVariables.TryGetValue("title", out string? title))
                {
                    MangaConnector? connector = _parent.GetConnector(connectorName);
                    if (connector is null || title.Length < 4)
                    {
                        SendResponse(HttpStatusCode.BadRequest, response);
                        return;
                    }
                    SendResponse(HttpStatusCode.OK, response, connector.GetPublications(title));
                }else
                    SendResponse(HttpStatusCode.BadRequest, response);
                break;
        }
    }

    private void HandlePost(HttpListenerRequest request, Stream content, HttpListenerResponse response)
    {
        
    }

    private void HandleDelete(HttpListenerRequest request, Stream content, HttpListenerResponse response)
    {
        
    }

    private void SendResponse(HttpStatusCode statusCode, HttpListenerResponse response, object? content = null)
    {
        Log($"Response: {statusCode} {content}");
        response.StatusCode = (int)statusCode;
        response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
        response.AddHeader("Access-Control-Allow-Methods", "GET, POST, DELETE");
        response.AddHeader("Access-Control-Max-Age", "1728000");
        response.AppendHeader("Access-Control-Allow-Origin", "*");
        response.ContentType = "application/json";
        try
        {
            response.OutputStream.Write(content is not null
                ? Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(content))
                : Array.Empty<byte>());
            response.OutputStream.Close();
        }
        catch (HttpListenerException)
        {
            
        }
    }
}