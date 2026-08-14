using System.Text.RegularExpressions;
using System.Threading.RateLimiting;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using Common.Datatypes;
using Common.Helpers;
using Common.Settings;
using Extensions.Data;

namespace Extensions.Extensions;

/// <summary>
/// Ported from https://github.com/keiyoushi/extensions-source/tree/main/src/en/weebcentral
/// </summary>
public sealed partial class WeebCentral : IDownloadExtension
{
    public Guid Identifier { get; init; } = Guid.Parse("0199a6b1-1c6f-7d2a-9a3e-3a9e6c5b1f10");

    public string Name { get; init; } = "WeebCentral";

    public Language[] SupportedLanguages { get; init; } = ["en"!];

    public string BaseUrl { get; init; } = "https://weebcentral.com";

    public string IconUrl { get; init; } = "https://weebcentral.com/static/images/apple-touch-icon.png";

    // The site sits behind Cloudflare, which RequestClient solves via FlareSolverr's
    // ClearanceHandler when EnvVars.FlareSolverrUrl is configured (see IsAvailable). Without
    // FlareSolverr there is no real way to pass the challenge; the browser-like User-Agent below
    // is only a best-effort fallback so this extension's own tests can still run standalone.
    public static bool IsAvailable => EnvVars.FlareSolverrUrl is not null;

    private static readonly RequestClient RequestClient = CreateRequestClient();

    private static RequestClient CreateRequestClient()
    {
        RequestClient client = new(new SlidingWindowRateLimiter(
            new SlidingWindowRateLimiterOptions()
            {
                AutoReplenishment = true,
                Window = TimeSpan.FromSeconds(2),
                SegmentsPerWindow = 1,
                PermitLimit = 1,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
        if (IsAvailable)
            return client;

        client.DefaultRequestHeaders.UserAgent.Clear();
        client.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36");
        return client;
    }

    private static readonly IBrowsingContext BrowsingContextInstance = AngleSharp.BrowsingContext.New(Configuration.Default);

    private const int FetchLimit = 32;

    [GeneratedRegex("[!#:(),-]")]
    private static partial Regex ExcludedSearchCharacters();

    #region Search

    public async Task<List<MangaInfo>?> SearchDownload(SearchQuery query, CancellationToken ct)
    {
        UriBuilder builder = new($"{BaseUrl}/search/data");
        builder.AddQueryParameter("text", ExcludedSearchCharacters().Replace(query.Title ?? string.Empty, " ").Trim())
            .AddQueryParameter("limit", FetchLimit.ToString())
            .AddQueryParameter("offset", "0")
            .AddQueryParameter("display_mode", "Full Display")
            .AddQueryParameter("sort", "Best Match")
            .AddQueryParameter("order", "Descending");
        if (query.Author is { } author)
            builder.AddQueryParameter("author", author);
        foreach (string tag in query.Tags ?? [])
            builder.AddQueryParameter("included_tag", tag);

        if (await GetDocument(builder.Uri, ct) is not { } document)
            return null;

        // Each result card is an <article> with two <section> children:
        // the first holds the cover thumbnail link, the second the title/details.
        List<Task<MangaInfo?>> tasks = document.QuerySelectorAll("article:has(> section > a)")
            .Select(element => ParseSearchResultEntry(element, ct))
            .ToList();
        await Task.WhenAll(tasks);

        return tasks
            .Where(t => t is { IsCompletedSuccessfully: true, Result: not null })
            .Select(t => t.Result!)
            .ToList();
    }

    private async Task<MangaInfo?> ParseSearchResultEntry(IElement card, CancellationToken ct)
    {
        if (card.QuerySelector("section > a") is not { } coverLink)
            return null;
        if (coverLink.GetAttribute("href") is not { } href)
            return null;
        Uri mangaUrl = ResolveUrl(href, card.Owner);
        if (mangaUrl.Segments.Length < 3)
            return null;

        string title = card.QuerySelector("a.line-clamp-1")?.TextContent.Trim()
            ?? coverLink.QuerySelector("img")?.GetAttribute("alt")?.Trim()
            ?? string.Empty;
        if (string.IsNullOrEmpty(title))
            return null;
        if (await FetchImage(SourceImage(coverLink), ct) is not { } cover)
            return null;

        string identifier = mangaUrl.Segments[2].Trim('/');
        return new MangaInfo(this.Identifier, title, mangaUrl.ToString(), identifier, cover);
    }

    #endregion

    #region Identifier

    public string? ParseIdentifierFromUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || uri.Segments.Length < 3)
            return null;
        if (!uri.Segments[1].Equals("series/", StringComparison.OrdinalIgnoreCase))
            return null;
        string identifier = uri.Segments[2].Trim('/');
        return string.IsNullOrEmpty(identifier) ? null : identifier;
    }

    #endregion

    #region Chapters

    public async Task<List<ChapterInfo>?> GetChapters(MangaInfo mangaInfo, CancellationToken ct)
    {
        Uri mangaUri = new(mangaInfo.Url);
        if (mangaUri.Segments.Length < 3)
            return null;
        string seriesId = mangaUri.Segments[2].Trim('/');
        Uri url = new($"{BaseUrl}/series/{seriesId}/full-chapter-list");

        if (await GetDocument(url, ct) is not { } document)
            return null;

        // Chapters are returned newest-first by the site.
        IHtmlCollection<IElement> chapters = document.QuerySelectorAll("div[x-data] > a");

        List<ChapterInfo> result = [];
        for (int index = 0; index < chapters.Length; index++)
        {
            IElement element = chapters[index];
            if (element.GetAttribute("href") is not { } href)
                continue;
            Uri chapterUrl = ResolveUrl(href, document);
            string name = ChapterName(element);

            // Titles look like "Chapter 12" or, for seasoned series, "S3 - Chapter 235";
            // fall back to reverse-index numbering when no explicit chapter number is present.
            string number = ChapterNumberRegex().Match(name) is { Success: true } match
                ? match.Groups[1].Value
                : (chapters.Length - index).ToString();

            string identifier = chapterUrl.Segments[^1].Trim('/');
            result.Add(new ChapterInfo(this.Identifier, number, chapterUrl.ToString(), identifier, Title: name));
        }

        return result;
    }

    private static string ChapterName(IElement element) =>
        element.QuerySelector("span.flex > span")?.TextContent.Trim() ?? string.Empty;

    [GeneratedRegex("""Chapter\s*(\d+(?:\.\d+)?)""", RegexOptions.IgnoreCase)]
    private static partial Regex ChapterNumberRegex();

    #endregion

    #region Images

    public async Task<List<ChapterImage>?> FetchChapterImages(ChapterInfo chapterInfo, CancellationToken ct)
    {
        UriBuilder builder = new(chapterInfo.Url)
        {
            Path = new Uri(chapterInfo.Url).AbsolutePath.TrimEnd('/') + "/images"
        };
        builder.AddQueryParameter("is_prev", "False")
            .AddQueryParameter("reading_style", "long_strip");

        if (await GetDocument(builder.Uri, ct) is not { } document)
            return null;

        List<string> imageUrls = document.QuerySelectorAll("section#chapter-images > img")
            .Select(element => element.GetAttribute("src"))
            .Where(src => !string.IsNullOrEmpty(src))
            .Select(src => ResolveUrl(src!, document).ToString())
            .ToList();

        if (imageUrls.Count == 0)
            return null;

        List<Task<TrangaImage?>> tasks = imageUrls.Select(url => FetchImage(url, ct)).ToList();
        await Task.WhenAll(tasks);

        if (tasks.Any(t => t is not { IsCompletedSuccessfully: true, Result: not null }))
            return null;

        return tasks.Select((t, index) => new ChapterImage(this.Identifier, chapterInfo.Identifier, index, t.Result!))
            .ToList();
    }

    #endregion

    #region Utilities

    private async Task<IDocument?> GetDocument(Uri url, CancellationToken ct)
    {
        // The chapter-list and page-list endpoints are htmx partials on the real site;
        // they can respond empty without these headers.
        using HttpRequestMessage request = new(HttpMethod.Get, url);
        request.Headers.Add("HX-Request", "true");
        request.Headers.Referrer = url;
        HttpResponseMessage response = await RequestClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            return null;
        string html = await response.Content.ReadAsStringAsync(ct);
        return await BrowsingContextInstance.OpenAsync(req => req.Content(html).Address(url.ToString()), ct);
    }

    private async Task<TrangaImage?> FetchImage(string? url, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(url))
            return null;
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

    // Mirrors Jsoup's `sourceImg()`: prefer the higher-resolution <source srcset> over <img src>.
    private static string? SourceImage(IElement element)
    {
        if (element.QuerySelector("source")?.GetAttribute("srcset") is { } srcset)
            return srcset.Replace("small", "normal");
        return element.QuerySelector("img")?.GetAttribute("src") is { } src ? ResolveUrl(src, element.Owner).ToString() : null;
    }

    private static Uri ResolveUrl(string href, IDocument? document) =>
        document is not null ? new Uri(new Uri(document.BaseUri), href) : new Uri(href);

    #endregion
}
