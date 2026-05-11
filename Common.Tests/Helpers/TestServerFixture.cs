using System.Net;
using System.Net.Sockets;


[assembly: AssemblyFixture(typeof(TestServerFixture))]
public sealed class TestServerFixture : IDisposable
{
    private readonly HttpListener _listener = new()
    {
        Prefixes = { $"http://*:{Port}/" }
    };

    private static readonly int Port = 8080;
    public static readonly string BaseUrl = $"http://localhost:{Port}/";

    public TestServerFixture()
    {
        _listener.Start();
        _ = AcceptConnections(Xunit.TestContext.Current.CancellationToken);
    }

    private async Task AcceptConnections(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            HttpListenerContext ctx = await _listener.GetContextAsync();
            ctx.Response.StatusCode = 200;
            ctx.Response.Close();
        }
    }

    private static int GetAvailablePort()
    {
        using Socket s = new (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        s.Bind(IPEndPoint.Parse("127.0.0.1:0"));
        return ((IPEndPoint)s.LocalEndPoint!).Port;
    }

    public void Dispose()
    {
        ((IDisposable)_listener).Dispose();
    }
}