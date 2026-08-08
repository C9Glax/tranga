using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Common.Helpers;
using KomgaExtension = Extensions.Extensions.Komga;

namespace Extensions.Tests.Extensions;

public sealed class KomgaTests : Common.Tests.TrangaTest
{
    /// <summary>
    /// A single-purpose loopback HTTP server that always answers with a preconfigured
    /// status code and body, and captures the last request it received.
    /// </summary>
    private sealed class RecordingHttpServer : IDisposable
    {
        private readonly HttpListener _listener = new();
        public string BaseUrl { get; }
        public HttpListenerRequest? LastRequest { get; private set; }
        public string? LastAuthorizationHeader { get; private set; }
        public string? LastApiKeyHeader { get; private set; }

        private readonly HttpStatusCode _statusCode;
        private readonly string? _body;

        public RecordingHttpServer(HttpStatusCode statusCode, string? body = null)
        {
            _statusCode = statusCode;
            _body = body;
            int port = GetAvailablePort();
            BaseUrl = $"http://localhost:{port}/";
            _listener.Prefixes.Add(BaseUrl);
            _listener.Start();
            _ = AcceptConnections();
        }

        private async Task AcceptConnections()
        {
            try
            {
                while (_listener.IsListening)
                {
                    HttpListenerContext ctx = await _listener.GetContextAsync();
                    LastRequest = ctx.Request;
                    LastAuthorizationHeader = ctx.Request.Headers["Authorization"];
                    LastApiKeyHeader = ctx.Request.Headers["X-API-Key"];

                    ctx.Response.StatusCode = (int)_statusCode;
                    if (_body is not null)
                    {
                        ctx.Response.ContentType = "application/json";
                        byte[] bytes = Encoding.UTF8.GetBytes(_body);
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

    private static RequestClient GetRequestClient(KomgaExtension komga)
    {
        FieldInfo? field = typeof(KomgaExtension).GetField("_komgaRequestClient", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        object? value = field.GetValue(komga);
        Assert.IsType<RequestClient>(value);
        return (RequestClient)value;
    }

    [Fact]
    public void TwoInstancesWithDifferentApiKeysSendDistinctHeaders()
    {
        KomgaExtension first = new("http://localhost/", "api-key-one");
        KomgaExtension second = new("http://localhost/", "api-key-two");

        RequestClient firstClient = GetRequestClient(first);
        RequestClient secondClient = GetRequestClient(second);

        IEnumerable<string> firstHeader = firstClient.DefaultRequestHeaders.GetValues("X-API-Key");
        IEnumerable<string> secondHeader = secondClient.DefaultRequestHeaders.GetValues("X-API-Key");

        Assert.Equal("api-key-one", Assert.Single(firstHeader));
        Assert.Equal("api-key-two", Assert.Single(secondHeader));
        Assert.NotSame(firstClient, secondClient);
    }

    [Fact]
    public async Task MintApiKeySendsBasicAuthHeaderAndReturnsKey()
    {
        const string responseBody = """
        {
            "comment": "Tranga",
            "createdDate": "2024-01-01T00:00:00Z",
            "id": "some-id",
            "key": "minted-api-key-value",
            "lastModifiedDate": "2024-01-01T00:00:00Z",
            "userId": "user-id"
        }
        """;
        using RecordingHttpServer server = new(HttpStatusCode.OK, responseBody);

        string key = await KomgaExtension.MintApiKey(server.BaseUrl, "someuser", "somepassword", ct);

        Assert.Equal("minted-api-key-value", key);
        Assert.NotNull(server.LastAuthorizationHeader);
        string expected = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("someuser:somepassword"));
        Assert.Equal(expected, server.LastAuthorizationHeader);
    }

    [Fact]
    public async Task MintApiKeyThrowsOnUnauthorized()
    {
        using RecordingHttpServer server = new(HttpStatusCode.Unauthorized);

        await Assert.ThrowsAnyAsync<Exception>(() => KomgaExtension.MintApiKey(server.BaseUrl, "someuser", "wrongpassword", ct));
    }
}
