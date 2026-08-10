using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using Common.Helpers;
using KomgaExtension = Extensions.Extensions.Komga;
using KomgaSeries = Extensions.Extensions.KomgaSeries;

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
        public byte[]? LastRequestBody { get; private set; }
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

                    using (MemoryStream bodyBuffer = new())
                    {
                        await ctx.Request.InputStream.CopyToAsync(bodyBuffer);
                        LastRequestBody = bodyBuffer.ToArray();
                    }

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

    [Fact]
    public async Task MintApiKeyDoesNotDoubleSlashPathWhenBaseUrlHasTrailingSlash()
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

        // RecordingHttpServer.BaseUrl always has a trailing slash.
        await KomgaExtension.MintApiKey(server.BaseUrl, "someuser", "somepassword", ct);

        Assert.NotNull(server.LastRequest);
        Assert.DoesNotContain("//", server.LastRequest!.Url!.AbsolutePath);
    }

    [Fact]
    public async Task GetSeriesList_TolerateExplicitNullOnNonNullableOptionalField()
    {
        // Regression test: Komga's OpenAPI spec doesn't mark every optional field as nullable
        // (e.g. SeriesMetadataDto.AgeRating is a non-nullable int), but a real Komga instance
        // legitimately serializes "ageRating": null for series without an age rating set, which
        // used to throw a JsonSerializationException and take down the whole GetSeriesList call.
        const string responseBody = """
        {
            "content": [
                {
                    "id": "series-1",
                    "name": "Some Series",
                    "libraryId": "komga-library-id",
                    "booksCount": 0,
                    "booksInProgressCount": 0,
                    "booksReadCount": 0,
                    "booksUnreadCount": 0,
                    "created": "2024-01-01T00:00:00Z",
                    "deleted": false,
                    "fileLastModified": "2024-01-01T00:00:00Z",
                    "lastModified": "2024-01-01T00:00:00Z",
                    "oneshot": false,
                    "url": "/some/path",
                    "booksMetadata": {
                        "authors": [],
                        "created": "2024-01-01T00:00:00Z",
                        "lastModified": "2024-01-01T00:00:00Z",
                        "summary": "",
                        "summaryNumber": "",
                        "tags": []
                    },
                    "metadata": {
                        "ageRating": null,
                        "ageRatingLock": false,
                        "alternateTitles": [],
                        "alternateTitlesLock": false,
                        "created": "2024-01-01T00:00:00Z",
                        "genres": [],
                        "genresLock": false,
                        "language": "",
                        "languageLock": false,
                        "lastModified": "2024-01-01T00:00:00Z",
                        "links": [],
                        "linksLock": false,
                        "publisher": "",
                        "publisherLock": false,
                        "readingDirection": "",
                        "readingDirectionLock": false,
                        "sharingLabels": [],
                        "sharingLabelsLock": false,
                        "status": "",
                        "statusLock": false,
                        "summary": "Series summary",
                        "summaryLock": false,
                        "tags": [],
                        "tagsLock": false,
                        "title": "",
                        "titleLock": false,
                        "titleSort": "",
                        "titleSortLock": false,
                        "totalBookCount": 0,
                        "totalBookCountLock": false
                    }
                }
            ]
        }
        """;
        using RecordingHttpServer server = new(HttpStatusCode.OK, responseBody);
        KomgaExtension extension = new(server.BaseUrl, "api-key");

        KomgaSeries[] series = await extension.GetSeriesList(ct);

        KomgaSeries onlySeries = Assert.Single(series);
        Assert.Equal("series-1", (string)onlySeries.Id);
        Assert.Equal("Some Series", onlySeries.Name);
        Assert.Equal("Series summary", onlySeries.Summary);
    }

    [Fact]
    public async Task GetSeriesListScopedToLibrary_SendsLibraryIdFilter()
    {
        const string responseBody = """
        {
            "content": []
        }
        """;
        using RecordingHttpServer server = new(HttpStatusCode.OK, responseBody);
        KomgaExtension extension = new(server.BaseUrl, "api-key");

        KomgaSeries[] series = await extension.GetSeriesList("the-tranga-library-id", ct);

        Assert.Empty(series);
        Assert.NotNull(server.LastRequest);
        Assert.Contains("library_id=the-tranga-library-id", server.LastRequest!.Url!.Query);
    }

    [Fact]
    public async Task UpdateSeriesPoster_SendsFileNameAndContentType()
    {
        // Regression test: the multipart file part used to be built from just the raw stream, which
        // defaults to filename "no_name_provided" and content type "application/octet-stream" -
        // Komga silently ignores poster uploads that don't look like a real image file.
        using RecordingHttpServer server = new(HttpStatusCode.OK);
        KomgaExtension extension = new(server.BaseUrl, "api-key");
        TrangaImage image = new();
        await image.WriteAsync("fake-image-bytes"u8.ToArray(), ct);
        image.Position = 0;

        await extension.UpdateSeriesPoster("series-1", "cover.jpg", "image/jpeg", image, ct);

        Assert.NotNull(server.LastRequestBody);
        string body = Encoding.Latin1.GetString(server.LastRequestBody);
        Assert.Contains("filename=cover.jpg", body);
        Assert.Contains("Content-Type: image/jpeg", body);
    }
}
