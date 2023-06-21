using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Logging;
using Newtonsoft.Json;
using Tranga;

namespace API;

public class Server
{
    private readonly HttpListener _listener = new ();
    private readonly RequestHandler _requestHandler;
    internal readonly Logger? logger;

    private readonly Regex _validUrl =
        new (@"https?:\/\/(www\.)?[-A-z0-9]{1,256}(\.[-a-zA-Z0-9]{1,6})?(:[0-9]{1,5})?(\/{1}[A-z0-9()@:%_\+.~#?&=]+)*\/?");
    public Server(int port, TaskManager taskManager, Logger? logger = null)
    {
        this.logger = logger;
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            this._listener.Prefixes.Add($"http://*:{port}/");
        else
            this._listener.Prefixes.Add($"http://localhost:{port}/");
        this._requestHandler = new RequestHandler(taskManager, this);
        Listen();
    }

    private void Listen()
    {
        this._listener.Start();
        foreach (string prefix in this._listener.Prefixes)
            this.logger?.WriteLine(this.GetType().ToString(), $"Listening on {prefix}");
        while (this._listener.IsListening)
        {
            HttpListenerContext context = this._listener.GetContextAsync().Result;
            Task t = new (() =>
            {
                HandleContext(context);
            });
            t.Start();
        }
    }

    private void HandleContext(HttpListenerContext context)
    {
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;
        //logger?.WriteLine(this.GetType().ToString(), $"New request: {request.HttpMethod} {request.Url}");

        if (!_validUrl.IsMatch(request.Url!.ToString()))
        {
            SendResponse(HttpStatusCode.BadRequest, response);
            return;
        }

        if (request.HttpMethod == "OPTIONS")
        {
            SendResponse(HttpStatusCode.OK, response);
        }
        else
        {
            _requestHandler.HandleRequest(request, response);
        }
    }

    internal void SendResponse(HttpStatusCode statusCode, HttpListenerResponse response, object? content = null)
    {
        if (!response.OutputStream.CanWrite)
            return;
        //logger?.WriteLine(this.GetType().ToString(), $"Sending response: {statusCode}");
        response.StatusCode = (int)statusCode;
        response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
        response.AddHeader("Access-Control-Allow-Methods", "GET, POST, DELETE");
        response.AddHeader("Access-Control-Max-Age", "1728000");
        response.AppendHeader("Access-Control-Allow-Origin", "*");
        response.ContentType = "application/json";
        response.OutputStream.Write(content is not null
            ? Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(content))
            : Array.Empty<byte>());
        response.OutputStream.Close();
        
    }
}