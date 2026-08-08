using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Common.Tests.Helpers;

/// <summary>
/// A single-purpose loopback HTTP server that always answers with a preconfigured
/// status code and body, for tests that need control over the response RequestClient sees.
/// </summary>
internal sealed class MockHttpServer : IDisposable
{
    private readonly HttpListener _listener = new();
    public string BaseUrl { get; }

    public MockHttpServer(HttpStatusCode statusCode, string? body = null, string contentType = "application/json")
    {
        int port = GetAvailablePort();
        BaseUrl = $"http://localhost:{port}/";
        _listener.Prefixes.Add(BaseUrl);
        _listener.Start();
        _ = AcceptConnections(statusCode, body, contentType);
    }

    private async Task AcceptConnections(HttpStatusCode statusCode, string? body, string contentType)
    {
        try
        {
            while (_listener.IsListening)
            {
                HttpListenerContext ctx = await _listener.GetContextAsync();
                ctx.Response.StatusCode = (int)statusCode;
                if (body is not null)
                {
                    ctx.Response.ContentType = contentType;
                    byte[] bytes = Encoding.UTF8.GetBytes(body);
                    await ctx.Response.OutputStream.WriteAsync(bytes);
                }

                ctx.Response.Close();
            }
        }
        catch (HttpListenerException)
        {
            // Listener was stopped/disposed while awaiting a connection.
        }
        catch (ObjectDisposedException)
        {
            // Listener was disposed while awaiting a connection.
        }
    }

    private static int GetAvailablePort()
    {
        using Socket s = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        s.Bind(IPEndPoint.Parse("127.0.0.1:0"));
        return ((IPEndPoint)s.LocalEndPoint!).Port;
    }

    public void Dispose() => ((IDisposable)_listener).Dispose();
}