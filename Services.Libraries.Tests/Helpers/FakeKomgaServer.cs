using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Services.Libraries.Tests.Helpers;

/// <summary>
/// A loopback HTTP server that answers requests based on a caller-supplied responder function,
/// keyed by the request path. Used to stand in for a real Komga instance in endpoint tests
/// without needing a live server. Local to Services.Libraries.Tests, modeled after the
/// RecordingHttpServer pattern in Extensions.Tests/Extensions/KomgaTests.cs.
/// </summary>
public sealed class FakeKomgaServer : IDisposable
{
    private readonly HttpListener _listener = new();
    public string BaseUrl { get; }

    private readonly Func<string, (HttpStatusCode StatusCode, string? Body)> _responder;

    public FakeKomgaServer(Func<string, (HttpStatusCode StatusCode, string? Body)> responder)
    {
        _responder = responder;
        int port = GetAvailablePort();
        BaseUrl = $"http://localhost:{port}/";
        _listener.Prefixes.Add(BaseUrl);
        _listener.Start();
        _ = AcceptConnections();
    }

    /// <summary>
    /// Convenience constructor for tests that only need a single fixed response regardless of path.
    /// </summary>
    public FakeKomgaServer(HttpStatusCode statusCode, string? body = null)
        : this(_ => (statusCode, body))
    {
    }

    private async Task AcceptConnections()
    {
        try
        {
            while (_listener.IsListening)
            {
                HttpListenerContext ctx = await _listener.GetContextAsync();
                (HttpStatusCode statusCode, string? body) = _responder(ctx.Request.Url?.AbsolutePath ?? string.Empty);

                ctx.Response.StatusCode = (int)statusCode;
                if (body is not null)
                {
                    ctx.Response.ContentType = "application/json";
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

    public const string ValidApiKeyMintResponseBody = """
    {
        "comment": "Tranga",
        "createdDate": "2024-01-01T00:00:00Z",
        "id": "some-id",
        "key": "minted-api-key-value",
        "lastModifiedDate": "2024-01-01T00:00:00Z",
        "userId": "user-id"
    }
    """;

    public const string ValidLibraryCreationResponseBody = """
    {
        "analyzeDimensions": true,
        "convertToCbz": true,
        "emptyTrashAfterScan": true,
        "hashFiles": true,
        "hashKoreader": false,
        "hashPages": true,
        "id": "komga-library-id",
        "importBarcodeIsbn": false,
        "importComicInfoBook": true,
        "importComicInfoCollection": false,
        "importComicInfoReadList": false,
        "importComicInfoSeries": false,
        "importComicInfoSeriesAppendVolume": false,
        "importEpubBook": false,
        "importEpubSeries": false,
        "importLocalArtwork": true,
        "importMylarSeries": false,
        "name": "Tranga",
        "repairExtensions": false,
        "root": "/tranga",
        "scanCbx": true,
        "scanDirectoryExclusions": [],
        "scanEpub": false,
        "scanForceModifiedTime": false,
        "scanInterval": "HOURLY",
        "scanOnStartup": true,
        "scanPdf": false,
        "seriesCover": "FIRST",
        "unavailable": false
    }
    """;

    public const string EmptySeriesListResponseBody = """
    {
        "content": []
    }
    """;
}
