using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Extensions.Tests.Custom;

/// <summary>
/// A loopback "website" for the selector engine tests: serves preconfigured responses per path (and query) and
/// remembers the requests it received so tests can check headers.
/// </summary>
internal sealed class FixtureSite : IDisposable
{
    internal sealed record Response(string Body, string ContentType = "text/html; charset=utf-8", HttpStatusCode Status = HttpStatusCode.OK,
        string? RedirectTo = null, byte[]? Bytes = null);

    internal sealed record ReceivedRequest(string PathAndQuery, string? Referer, string? UserAgent, string? Cookie, string? Accept = null);

    private readonly HttpListener _listener = new();
    private readonly Dictionary<string, Response> _routes = new();
    private readonly List<ReceivedRequest> _requests = [];

    /// <summary>Root URL without trailing slash, e.g. <c>http://localhost:5555</c></summary>
    public string BaseUrl { get; }

    public IReadOnlyList<ReceivedRequest> Requests
    {
        get
        {
            lock (_requests)
                return [.. _requests];
        }
    }

    public FixtureSite()
    {
        using Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        int port = ((IPEndPoint)socket.LocalEndPoint!).Port;
        BaseUrl = $"http://localhost:{port}";
        _listener.Prefixes.Add($"{BaseUrl}/");
        _listener.Start();
        _ = Serve();
    }

    /// <summary>Serves <paramref name="response"/> for <paramref name="pathAndQuery"/> (e.g. <c>/search?q=x</c>).</summary>
    public FixtureSite Map(string pathAndQuery, Response response)
    {
        lock (_routes)
            _routes[pathAndQuery] = response;
        return this;
    }

    public FixtureSite Html(string pathAndQuery, string html) => Map(pathAndQuery, new Response(html));

    private async Task Serve()
    {
        try
        {
            while (_listener.IsListening)
            {
                HttpListenerContext context = await _listener.GetContextAsync();
                lock (_requests)
                    _requests.Add(new ReceivedRequest(context.Request.Url!.PathAndQuery, context.Request.Headers["Referer"],
                        context.Request.Headers["User-Agent"], context.Request.Headers["Cookie"], context.Request.Headers["Accept"]));

                Response? response;
                lock (_routes)
                    _routes.TryGetValue(context.Request.Url!.PathAndQuery, out response);

                if (response is null)
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                else
                {
                    context.Response.StatusCode = (int)response.Status;
                    if (response.RedirectTo is not null)
                        context.Response.RedirectLocation = response.RedirectTo;
                    context.Response.ContentType = response.ContentType;
                    byte[] bytes = response.Bytes ?? Encoding.UTF8.GetBytes(response.Body);
                    await context.Response.OutputStream.WriteAsync(bytes);
                }

                context.Response.Close();
            }
        }
        catch (HttpListenerException)
        {
            // Stopped while waiting for a connection.
        }
        catch (ObjectDisposedException)
        {
            // Disposed while waiting for a connection.
        }
    }

    public void Dispose() => ((IDisposable)_listener).Dispose();
}
