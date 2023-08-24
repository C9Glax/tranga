using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;

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
                if(context.Request.HttpMethod == "OPTIONS")
                    SendResponse(HttpStatusCode.OK, context.Response);
                else
                    HandleRequest(context);
            });
            t.Start();
        }
    }

    private void HandleRequest(HttpListenerContext context)
    {
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;
        if(request.Url!.LocalPath.Contains("favicon"))
            SendResponse(HttpStatusCode.NoContent, response);
        
        SendResponse(HttpStatusCode.NotFound, response);
    }

    private void SendResponse(HttpStatusCode statusCode, HttpListenerResponse response, object? content = null)
    {
        //logger?.WriteLine(this.GetType().ToString(), $"Sending response: {statusCode}");
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