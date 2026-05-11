using System.Net;
using Common.Helpers;

namespace Common.Tests.Helpers;

public class RequestClientTests : TrangaTest
{
    [Fact]
    public async Task SuccessfulRequest()
    { 
        RequestClient client = new();
        using TestServer server = new();
        HttpRequestMessage request = new(HttpMethod.Get, TestServer.BaseUrl);

        HttpResponseMessage response = await client.SendAsync(request, ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    internal sealed class TestServer : IDisposable
    {
        private readonly HttpListener _listener = new()
        {
            Prefixes = { $"http://*:{Port}/" }
        };

        private const int Port = 8080;
        public static readonly string BaseUrl = $"http://localhost:{Port}/";

        public TestServer()
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

        public void Dispose()
        {
            ((IDisposable)_listener).Dispose();
        }
    }
}