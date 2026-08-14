using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.RateLimiting;
using Common.Helpers;
using Common.Settings;

namespace Extensions.Extensions.Suwayomi;

/// <summary>
/// Talks to the Suwayomi sidecar's GraphQL endpoint.
/// <para>
/// Suwayomi runs Tachiyomi/Mihon extension APKs (e.g. the whole keiyoushi repository) on the JVM behind an Android
/// compatibility layer, which is why Tranga proxies through it instead of loading the extensions itself.
/// </para>
/// <para>
/// Every method returns <see langword="null"/> on any failure, matching the failure signal used across
/// <see cref="IDownloadExtension"/>. Nothing here throws for an unreachable or erroring sidecar.
/// </para>
/// </summary>
internal static class SuwayomiClient
{
    /// <summary>The sidecar is normally local, so the limiter only exists to stop a wide search fan-out from flooding it.</summary>
    private static readonly RequestClient RequestClient = new(new SlidingWindowRateLimiter(
        new SlidingWindowRateLimiterOptions()
        {
            AutoReplenishment = true,
            Window = TimeSpan.FromSeconds(1),
            SegmentsPerWindow = 1,
            PermitLimit = 30,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        }));

    /// <summary>Base address of the sidecar, without a trailing slash.</summary>
    internal static string BaseUrl => EnvVars.SuwayomiUrl.TrimEnd('/');

    private const string SourceFields = "id name lang displayName iconUrl homeUrl isNsfw supportsLatest";
    private const string ExtensionFields = "pkgName name lang iconUrl versionName isNsfw isInstalled isObsolete hasUpdate";
    private const string MangaFields = "id sourceId url title thumbnailUrl description author artist genre realUrl";
    private const string ChapterFields = "id url name chapterNumber scanlator sourceOrder";

    /// <summary>Server name/version, or <see langword="null"/> when the sidecar cannot be reached. Doubles as the reachability probe.</summary>
    internal static async Task<AboutServerPayload?> GetAboutAsync(CancellationToken ct) =>
        (await ExecuteAsync<AboutServerData>("query { aboutServer { name version buildType } }", ct))?.AboutServer;

    /// <summary>The sources of every currently installed extension.</summary>
    internal static async Task<SuwayomiSourceDto[]?> GetSourcesAsync(CancellationToken ct) =>
        (await ExecuteAsync<SourcesData>($"query {{ sources {{ nodes {{ {SourceFields} }} }} }}", ct))?.Sources?.Nodes;

    /// <summary>Extensions already known to Suwayomi's database, without contacting the configured extension stores.</summary>
    internal static async Task<SuwayomiExtensionDto[]?> GetExtensionsAsync(CancellationToken ct) =>
        (await ExecuteAsync<ExtensionsData>($"query {{ extensions {{ nodes {{ {ExtensionFields} }} }} }}", ct))?.Extensions?.Nodes;

    /// <summary>Re-reads the configured extension stores (keiyoushi) and returns the refreshed catalogue. Slow — it hits the network.</summary>
    internal static async Task<SuwayomiExtensionDto[]?> FetchExtensionsAsync(CancellationToken ct) =>
        (await ExecuteAsync<FetchExtensionsData>($"mutation {{ fetchExtensions(input: {{}}) {{ extensions {{ {ExtensionFields} }} }} }}", ct))
        ?.FetchExtensions?.Extensions;

    /// <summary>Installs, updates or uninstalls a single extension, identified by its package name.</summary>
    internal static async Task<SuwayomiExtensionDto?> SetExtensionStateAsync(string pkgName, SuwayomiExtensionAction action, CancellationToken ct)
    {
        string patch = action switch
        {
            SuwayomiExtensionAction.Install => "install: true",
            SuwayomiExtensionAction.Update => "update: true",
            SuwayomiExtensionAction.Uninstall => "uninstall: true",
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null)
        };
        string document = $"mutation {{ updateExtension(input: {{ id: {Literal(pkgName)}, patch: {{ {patch} }} }}) {{ extension {{ {ExtensionFields} }} }} }}";
        return (await ExecuteAsync<UpdateExtensionData>(document, ct))?.UpdateExtension?.Extension;
    }

    /// <summary>Runs a search against a single source. Suwayomi persists every returned manga, which is what makes the follow-up lookups possible.</summary>
    internal static async Task<SuwayomiMangaDto[]?> SearchSourceAsync(string sourceId, string query, int page, CancellationToken ct)
    {
        string document = $"mutation {{ fetchSourceManga(input: {{ source: {Literal(sourceId)}, type: SEARCH, page: {page}, query: {Literal(query)} }}) " +
                          $"{{ hasNextPage mangas {{ {MangaFields} }} }} }}";
        return (await ExecuteAsync<FetchSourceMangaData>(document, ct))?.FetchSourceManga?.Mangas;
    }

    /// <summary>Resolves a source-relative manga url to Suwayomi's local row id, which the fetch mutations require.</summary>
    internal static async Task<SuwayomiMangaDto?> ResolveMangaAsync(string sourceId, string url, CancellationToken ct)
    {
        string document = $"query {{ mangas(condition: {{ sourceId: {Literal(sourceId)}, url: {Literal(url)} }}) {{ nodes {{ {MangaFields} }} }} }}";
        return (await ExecuteAsync<MangasData>(document, ct))?.Mangas?.Nodes?.FirstOrDefault();
    }

    /// <summary>Asks the source for the manga's current chapter list and returns it.</summary>
    internal static async Task<SuwayomiChapterDto[]?> FetchChaptersAsync(int mangaId, CancellationToken ct)
    {
        string document = $"mutation {{ fetchMangaAndChapters(input: {{ id: {mangaId}, fetchManga: true, fetchChapters: true }}) " +
                          $"{{ manga {{ {MangaFields} }} chapters {{ {ChapterFields} }} }} }}";
        return (await ExecuteAsync<FetchMangaAndChaptersData>(document, ct))?.FetchMangaAndChapters?.Chapters;
    }

    /// <summary>Resolves a source-relative chapter url to Suwayomi's local row id.</summary>
    internal static async Task<SuwayomiChapterDto?> ResolveChapterAsync(int mangaId, string url, CancellationToken ct)
    {
        string document = $"query {{ chapters(condition: {{ mangaId: {mangaId}, url: {Literal(url)} }}) {{ nodes {{ {ChapterFields} }} }} }}";
        return (await ExecuteAsync<ChaptersData>(document, ct))?.Chapters?.Nodes?.FirstOrDefault();
    }

    /// <summary>Returns the chapter's page image urls, relative to <see cref="BaseUrl"/>.</summary>
    internal static async Task<string[]?> GetChapterPagesAsync(int chapterId, CancellationToken ct)
    {
        string document = $"mutation {{ fetchChapterPages(input: {{ chapterId: {chapterId} }}) {{ pages }} }}";
        return (await ExecuteAsync<FetchChapterPagesData>(document, ct))?.FetchChapterPages?.Pages;
    }

    /// <summary>Downloads an image the sidecar serves (cover thumbnails and chapter pages are both proxied by Suwayomi).</summary>
    internal static async Task<TrangaImage?> GetImageAsync(string? relativeOrAbsoluteUrl, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(relativeOrAbsoluteUrl))
            return null;

        string url = relativeOrAbsoluteUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? relativeOrAbsoluteUrl
            : $"{BaseUrl}/{relativeOrAbsoluteUrl.TrimStart('/')}";

        try
        {
            using HttpRequestMessage request = new(HttpMethod.Get, url);
            request.Headers.Add("Accept", "image/avif,image/webp,*/*");
            HttpResponseMessage response = await RequestClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                return null;
            TrangaImage image = new();
            Stream data = await response.Content.ReadAsStreamAsync(ct);
            await data.CopyToAsync(image, ct);
            return image;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return null;
        }
    }

    /// <summary>
    /// Rewrites a sidecar-relative url so a browser can load it through the YARP gateway, which serves Suwayomi under
    /// <c>/suwayomi</c>. Absolute urls are passed through untouched.
    /// </summary>
    internal static string ToGatewayUrl(string? suwayomiUrl)
    {
        if (string.IsNullOrEmpty(suwayomiUrl))
            return string.Empty;
        return suwayomiUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? suwayomiUrl
            : $"/suwayomi/{suwayomiUrl.TrimStart('/')}";
    }

    private static async Task<TData?> ExecuteAsync<TData>(string document, CancellationToken ct)
    {
        try
        {
            using HttpRequestMessage request = new(HttpMethod.Post, $"{BaseUrl}/api/graphql")
            {
                Content = JsonContent.Create(new GraphQlRequest(document), options: JsonSerializerOptions.Web)
            };
            GraphQlResponse<TData>? response = await RequestClient.SendAsyncAndParseJson<GraphQlResponse<TData>>(request, ct);

            // A GraphQL endpoint answers 200 even for query errors, so the envelope has to be checked explicitly.
            if (response is null || response.Errors is { Length: > 0 })
                return default;
            return response.Data;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException)
        {
            return default;
        }
    }

    /// <summary>
    /// Renders a GraphQL string literal. GraphQL and JSON share their string escape syntax, so the JSON serializer is
    /// a safe way to escape user-supplied search terms and package names into an inlined document.
    /// </summary>
    private static string Literal(string value) => JsonSerializer.Serialize(value);
}

/// <summary>The mutations Tranga performs against a Suwayomi extension.</summary>
internal enum SuwayomiExtensionAction
{
    Install,
    Update,
    Uninstall
}
