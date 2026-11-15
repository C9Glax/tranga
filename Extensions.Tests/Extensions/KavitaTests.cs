using System.Net;
using System.Net.Sockets;
using System.Text;
using Extensions.Data;
using Common.Helpers;
using KavitaExtension = Extensions.Extensions.Kavita;
using KavitaLibrary = Extensions.Extensions.KavitaLibrary;
using KavitaSeries = Extensions.Extensions.KavitaSeries;

namespace Extensions.Tests.Extensions;

public sealed class KavitaTests : Common.Tests.TrangaTest
{
    /// <summary>
    /// A single-purpose loopback HTTP server that always answers with a preconfigured
    /// status code and body, and captures the last request it received.
    /// </summary>
    private sealed class RecordingHttpServer : IDisposable
    {
        private readonly HttpListener _listener = new();
        public string BaseUrl { get; }
        public string? LastMethod { get; private set; }
        public string? LastPathAndQuery { get; private set; }
        public string? LastBody { get; private set; }
        public string? LastApiKeyHeader { get; private set; }

        private readonly HttpStatusCode _statusCode;
        private readonly string? _body;

        public RecordingHttpServer(HttpStatusCode statusCode, string? body = null)
        {
            _statusCode = statusCode;
            _body = body;
            using Socket s = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            s.Bind(IPEndPoint.Parse("127.0.0.1:0"));
            BaseUrl = $"http://localhost:{((IPEndPoint)s.LocalEndPoint!).Port}/";
            s.Close();
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
                    LastMethod = ctx.Request.HttpMethod;
                    LastPathAndQuery = ctx.Request.Url?.PathAndQuery;
                    LastApiKeyHeader = ctx.Request.Headers["x-api-key"];
                    using (StreamReader reader = new(ctx.Request.InputStream, Encoding.UTF8))
                        LastBody = await reader.ReadToEndAsync();

                    ctx.Response.StatusCode = (int)_statusCode;
                    if (_body is not null)
                    {
                        ctx.Response.ContentType = "application/json";
                        await ctx.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(_body));
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

        public void Dispose() => ((IDisposable)_listener).Dispose();
    }

    [Fact]
    public async Task GetLibraries_SendsApiKeyHeaderAndParsesLibraries()
    {
        using RecordingHttpServer server = new(HttpStatusCode.OK, """[{"id": 3, "name": "Manga"}, {"id": 7, "name": "Comics"}]""");
        KavitaExtension kavita = new(server.BaseUrl, "kavita-key");

        KavitaLibrary[] libraries = await kavita.GetLibraries(ct);

        Assert.Equal("GET", server.LastMethod);
        Assert.Equal("/api/Library/libraries", server.LastPathAndQuery);
        Assert.Equal("kavita-key", server.LastApiKeyHeader);
        Assert.Equal([new KavitaLibrary("3", "Manga"), new KavitaLibrary("7", "Comics")], libraries);
    }

    [Fact]
    public async Task GetLibraries_ThrowsOnUnauthorized()
    {
        using RecordingHttpServer server = new(HttpStatusCode.Unauthorized);
        KavitaExtension kavita = new(server.BaseUrl, "bad-key");

        await Assert.ThrowsAsync<HttpRequestException>(() => kavita.GetLibraries(ct));
    }

    [Fact]
    public async Task GetLibraries_TrailingSlashBaseUrlDoesNotProduceDoubleSlash()
    {
        using RecordingHttpServer server = new(HttpStatusCode.OK, "[]");
        KavitaExtension kavita = new(server.BaseUrl + "/", "key");

        await kavita.GetLibraries(ct);

        Assert.Equal("/api/Library/libraries", server.LastPathAndQuery);
    }

    [Fact]
    public async Task ScanLibrary_PostsScanForLibraryId()
    {
        using RecordingHttpServer server = new(HttpStatusCode.OK);
        KavitaExtension kavita = new(server.BaseUrl, "kavita-key");

        await kavita.ScanLibrary("42", ct);

        Assert.Equal("POST", server.LastMethod);
        Assert.Equal("/api/Library/scan?libraryId=42&force=false", server.LastPathAndQuery);
        Assert.Equal("kavita-key", server.LastApiKeyHeader);
    }

    [Fact]
    public async Task ScanLibrary_ThrowsOnServerError()
    {
        using RecordingHttpServer server = new(HttpStatusCode.InternalServerError);
        KavitaExtension kavita = new(server.BaseUrl, "kavita-key");

        await Assert.ThrowsAsync<HttpRequestException>(() => kavita.ScanLibrary("42", ct));
    }

    [Fact]
    public async Task GetSeriesList_FiltersByLibraryAndParsesSeries()
    {
        using RecordingHttpServer server = new(HttpStatusCode.OK, """[{"id": 11, "name": "One Piece"}, {"id": 12, "name": "Berserk"}]""");
        KavitaExtension kavita = new(server.BaseUrl, "kavita-key");

        KavitaSeries[] series = await kavita.GetSeriesList((StringIdentifier)"5", ct);

        Assert.Equal("POST", server.LastMethod);
        Assert.StartsWith("/api/Series/all-v2", server.LastPathAndQuery);
        Assert.Contains("\"field\":19", server.LastBody);
        Assert.Contains("\"value\":\"5\"", server.LastBody);
        Assert.Equal(["11", "12"], series.Select(s => (string)s.Id));
        Assert.Equal(["One Piece", "Berserk"], series.Select(s => s.Name));
    }

    [Fact]
    public async Task UpdateSeriesMetadataAndPoster_DoNotCallTheServer()
    {
        using RecordingHttpServer server = new(HttpStatusCode.OK);
        KavitaExtension kavita = new(server.BaseUrl, "kavita-key");

        await kavita.UpdateSeriesMetadata(new KavitaSeries("1", "Name", "Summary"), ct);
        await kavita.UpdateSeriesPoster("1", "cover.png", "image/png", new TrangaImage(), ct);

        Assert.Null(server.LastMethod);
    }

    [Fact]
    public async Task CreateTrangaLibrary_IsNotSupported()
    {
        KavitaExtension kavita = new("http://localhost", "key");

        await Assert.ThrowsAsync<NotSupportedException>(() => kavita.CreateTrangaLibrary(ct));
    }
}
