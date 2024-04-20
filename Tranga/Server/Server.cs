using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Tranga.Server;

public class Server : GlobalBase, IDisposable
{
    private readonly HttpListener _listener = new();
    private readonly Tranga _parent;
    private bool _running = true;

    public Server(Tranga parent) : base(parent)
    {
        this._parent = parent;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            this._listener.Prefixes.Add($"http://*:{settings.apiPortNumber}/");
        else
            this._listener.Prefixes.Add($"http://localhost:{settings.apiPortNumber}/");
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
            SendResponse(HttpStatusCode.OK, context.Response);
        if (request.Url!.LocalPath.Contains("favicon"))
            SendResponse(HttpStatusCode.NoContent, response);
        string path = Regex.Match(request.Url!.LocalPath, @"[A-z0-9]+(\/[A-z0-9]+)*").Value;

        if (!Regex.IsMatch(request.Url.LocalPath, "/v2(/.*)?"))
        {
            SendResponse(HttpStatusCode.NotFound, response);
            return;
        }
        
        Dictionary<string, string> requestVariables = GetRequestVariables(request.Url!.Query);  //Variables in the URI
        Dictionary<string, string> requestBody = GetRequestBody(request);                       //Variables in the JSON body
        Dictionary<string, string> requestParams = requestVariables.UnionBy(requestBody, v => v.Key)
            .ToDictionary(kv => kv.Key, kv => kv.Value); //The actual variable used for the API

        ValueTuple<HttpStatusCode, object?> responseMessage = request.HttpMethod switch
        {
            "GET" => HandleGet(path, requestParams),
            "POST" => HandlePost(path, requestParams),
            "DELETE" => HandleDelete(path, requestParams),
            _ => new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.MethodNotAllowed, null)
        };
        
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
            Log("No request body");
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
    
    private ValueTuple<HttpStatusCode, object?> HandleGet(string path, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not implemented.");
    }
    
    private ValueTuple<HttpStatusCode, object?> HandlePost(string path, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not implemented.");
    }
    
    private ValueTuple<HttpStatusCode, object?> HandleDelete(string path, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not implemented.");
    }


    public void Dispose()
    {
        _running = false;
        ((IDisposable)_listener).Dispose();
    }
}