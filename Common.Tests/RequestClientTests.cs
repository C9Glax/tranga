using System.Net;
using System.Threading.RateLimiting;

namespace Common.Tests;

public sealed class RequestClientTests : TestContext
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
    
    [Fact]
    public async Task RateLimitApplies()
    { 
        using TestServer server = new();
        RequestClient client = new(new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions()
        {
            AutoReplenishment = true,
            PermitLimit = 60,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            Window = TimeSpan.FromSeconds(1),
            SegmentsPerWindow = 1
        }));
        List<Task> requests = [];
        DateTime start = DateTime.Now;
        foreach (int _ in new int[120])
        {
            HttpRequestMessage request = new(HttpMethod.Get, $"{TestServer.BaseUrl}?{DateTime.Now}");
            requests.Add(client.SendAsync(request, ct));
        }

        await Task.WhenAll(requests);
        DateTime end = DateTime.Now;
        
        Assert.True(end - start > TimeSpan.FromSeconds(1));
    }

    private sealed class TestServer : IDisposable
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