using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.RateLimiting;
using Common.Datatypes;
using Common.Helpers;
using Common.Settings;
using Extensions.Data;
using Extensions.Helpers;
using FlareSolverrSharp;
using AngleSharp.Dom;

namespace Extensions.Custom;

/// <summary>Environment dependent behaviour of <see cref="SelectorDownloadExtension"/>, injectable for tests.</summary>
/// <param name="AllowPrivateHosts">Whether sources may talk to loopback/LAN addresses</param>
/// <param name="FlareSolverrUrl">The FlareSolverr instance sources with <see cref="SelectorDownloaderDefinition.UseFlareSolverr"/> use, if any</param>
public sealed record SelectorDownloaderOptions(bool AllowPrivateHosts, string? FlareSolverrUrl)
{
    /// <summary>Taken from the environment (<c>ALLOW_PRIVATE_SOURCE_HOSTS</c>, <c>FLARESOLVERR_URL</c>).</summary>
    public static SelectorDownloaderOptions FromEnvironment() => new(EnvVars.AllowPrivateSourceHosts, EnvVars.FlareSolverrUrl);
}

/// <summary>
/// A download extension that scrapes a website with the CSS/XPath selectors of a
/// <see cref="SelectorDownloaderDefinition"/>.
/// </summary>
public sealed class SelectorDownloadExtension : IDownloadExtension, IDisposable
{
    private const int MaxSearchResults = 10;
    private const int MaxChapterPages = 50;
    private const int MaxChapterImagePages = 500;
    private const int MaxTestRows = 25;
    private const int MaxTestSampledPages = 3;
    private const string DefaultImageAttributes = "data-src,data-lazy-src,src";
    private const string DefaultPageAttributes = "href,value";

    private static readonly Regex Placeholder = new(@"\{(\w+)\}", RegexOptions.Compiled, SelectorDownloaderDefinition.RegexTimeout);
    private static readonly Regex FirstNumber = new(@"\d+(?:\.\d+)?", RegexOptions.Compiled, SelectorDownloaderDefinition.RegexTimeout);

    private readonly SelectorDownloaderDefinition _definition;
    private readonly SelectorDownloaderOptions _options;
    private readonly Uri _baseUri;
    private readonly Regex? _seriesPattern;
    private readonly RequestClient _siteClient;
    private readonly RequestClient _assetClient;

    /// <inheritdoc />
    public Guid Identifier { get; init; }

    /// <inheritdoc />
    public string Name { get; init; }

    /// <inheritdoc />
    public string BaseUrl { get; init; }

    /// <inheritdoc />
    public string IconUrl { get; init; }

    /// <inheritdoc />
    public Language[] SupportedLanguages { get; init; }

    /// <param name="definition">A definition that passes <see cref="SelectorDownloaderDefinition.Validate()"/></param>
    /// <param name="options">Defaults to the environment</param>
    /// <exception cref="ArgumentException">The definition is invalid</exception>
    public SelectorDownloadExtension(SelectorDownloaderDefinition definition, SelectorDownloaderOptions? options = null)
        : this(definition, options, step: null)
    {
    }

    /// <summary>
    /// Creates an extension that is only valid for trying out one <paramref name="step"/> (the other sections of the
    /// definition may still be empty). Used by the live test panel.
    /// </summary>
    /// <exception cref="ArgumentException">The general settings or the chosen step are invalid</exception>
    public static SelectorDownloadExtension CreateForTest(SelectorDownloaderDefinition definition, SelectorDownloaderStep step,
        SelectorDownloaderOptions? options = null) => new(definition, options, step);

    private SelectorDownloadExtension(SelectorDownloaderDefinition definition, SelectorDownloaderOptions? options, SelectorDownloaderStep? step)
    {
        if (definition.Validate(step) is { Count: > 0 } errors)
            throw new ArgumentException(string.Join(" ", errors), nameof(definition));

        _definition = definition;
        _options = options ?? SelectorDownloaderOptions.FromEnvironment();
        _baseUri = new Uri(definition.BaseUrl);
        _seriesPattern = string.IsNullOrWhiteSpace(definition.SeriesUrlPattern)
            ? null
            : new Regex(definition.SeriesUrlPattern, RegexOptions.None, SelectorDownloaderDefinition.RegexTimeout);

        Identifier = definition.Id;
        Name = definition.Name;
        BaseUrl = definition.BaseUrl;
        IconUrl = definition.IconUrl is { Length: > 0 } icon ? icon : new Uri(_baseUri, "/favicon.ico").ToString();
        SupportedLanguages = definition.Languages.Select(l => new Language(l)).ToArray();

        // Only the website itself is throttled by the user's limit; image CDNs get a fixed, more generous one.
        _siteClient = new RequestClient(CreateHandler(), CreateLimiter(definition.RequestsPerSecond))
        {
            MaxResponseContentBufferSize = 64 * 1024 * 1024
        };
        _assetClient = new RequestClient(CreateHandler(), CreateLimiter(Math.Max(definition.RequestsPerSecond, 5)));
    }

    private HttpMessageHandler CreateHandler() =>
        _definition.UseFlareSolverr && _options.FlareSolverrUrl is { } flareSolverrUrl
            ? new HostGuard.HostCheckHandler(_options.AllowPrivateHosts, new ClearanceHandler(flareSolverrUrl) { MaxTimeout = 60000 })
            : HostGuard.CreateHandler(_options.AllowPrivateHosts);

    private static RateLimiter CreateLimiter(double requestsPerSecond) => new SlidingWindowRateLimiter(
        new SlidingWindowRateLimiterOptions
        {
            AutoReplenishment = true,
            // 0.5 requests per second = 1 request per 2 second window
            PermitLimit = requestsPerSecond >= 1 ? (int)Math.Ceiling(requestsPerSecond) : 1,
            Window = requestsPerSecond >= 1 ? TimeSpan.FromSeconds(1) : TimeSpan.FromSeconds(1 / requestsPerSecond),
            SegmentsPerWindow = 1,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });

    /// <inheritdoc />
    public void Dispose()
    {
        _siteClient.Dispose();
        _assetClient.Dispose();
    }

    #region Search

    /// <inheritdoc />
    public async Task<List<MangaInfo>?> SearchDownload(SearchQuery query, CancellationToken ct)
    {
        if (!SupportsLanguage(query.Language ?? Settings.DownloadLanguage) || string.IsNullOrWhiteSpace(query.Title))
            return [];

        if (await TryFetchDocument(SearchUrl(query.Title), ct) is not { Document: { } document } page)
            return null;

        List<SearchItem> items = ParseSearchItems(document, page.Url).Items.Take(MaxSearchResults).ToList();
        MangaInfo?[] results = await Task.WhenAll(items.Select(async item =>
        {
            TrangaImage? cover = await DownloadImage(new Uri(item.CoverUrl), _baseUri.ToString(), ct);
            // MANGA sites do not tell us the content rating, so nothing is flagged NSFW.
            return cover is null ? null : new MangaInfo(Identifier, item.Title, item.Url, item.Url, cover, Description: null, NSFW: false);
        }));
        return results.OfType<MangaInfo>().ToList();
    }

    private Uri SearchUrl(string title) => BuildUrl(_definition.Search.UrlTemplate,
        new Dictionary<string, string> { ["query"] = Uri.EscapeDataString(title.Trim()), ["page"] = "1" })!;

    private bool SupportsLanguage(Language language) => _definition.Languages.Any(language.Equals);

    private sealed record SearchItem(string Title, string Url, string CoverUrl);

    private ParseResult<SearchItem> ParseSearchItems(IDocument document, Uri pageUrl)
    {
        SearchSpec spec = _definition.Search;
        List<INode> nodes = SelectorEvaluator.SelectAll(document, spec.ItemSelector, relative: false);
        List<SearchItem> items = [];
        int missing = 0;
        foreach (INode node in nodes)
        {
            if (SelectorEvaluator.ExtractText(node, spec.Title) is not { } title ||
                SelectorEvaluator.ExtractUrl(node, spec.Url, pageUrl) is not { } url ||
                SelectorEvaluator.ExtractUrl(node, spec.Cover, pageUrl) is not { } cover)
            {
                missing++;
                continue;
            }

            items.Add(new SearchItem(title, url, cover));
        }

        return new ParseResult<SearchItem>(items.DistinctBy(i => i.Url).ToList(), nodes.Count, missing);
    }

    #endregion

    #region Identifier

    /// <inheritdoc />
    public string? ParseIdentifierFromUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) || uri.Scheme is not ("http" or "https"))
            return null;
        if (!HostWithoutWww(uri).Equals(HostWithoutWww(_baseUri), StringComparison.OrdinalIgnoreCase))
            return null;

        string identifier = uri.GetLeftPart(UriPartial.Query);
        try
        {
            return _seriesPattern is not null && !_seriesPattern.IsMatch(identifier) ? null : identifier;
        }
        catch (RegexMatchTimeoutException)
        {
            return null;
        }
    }

    private static string HostWithoutWww(Uri uri) => uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? uri.Host[4..] : uri.Host;

    #endregion

    #region Chapters

    /// <inheritdoc />
    public async Task<List<ChapterInfo>?> GetChapters(MangaInfo mangaInfo, CancellationToken ct)
    {
        if (SeriesVariables(mangaInfo.Identifier) is not { } variables ||
            BuildUrl(_definition.Chapters.UrlTemplate, variables) is not { } firstUrl)
            return null;

        List<ChapterInfo> chapters = [];
        HashSet<string> visited = [];
        Uri? next = firstUrl;
        for (int pages = 0; next is not null && pages < MaxChapterPages && visited.Add(next.ToString()); pages++)
        {
            if (await TryFetchDocument(next, ct) is not { Document: { } document } page)
                return pages == 0 ? null : chapters;

            chapters.AddRange(ParseChapterItems(document, page.Url).Items);
            next = ParseNextPage(document, page.Url);
        }

        return chapters.DistinctBy(c => c.Url).ToList();
    }

    /// <summary>The placeholders available in the chapter list URL template for a series, or null if it is not a series of this source.</summary>
    private Dictionary<string, string>? SeriesVariables(string seriesUrl)
    {
        Dictionary<string, string> variables = new() { ["url"] = seriesUrl };
        if (_seriesPattern is null)
            return variables;

        try
        {
            if (_seriesPattern.Match(seriesUrl) is not { Success: true } match)
                return null;
            foreach (string group in _seriesPattern.GetGroupNames().Where(n => !int.TryParse(n, out _)))
                variables[group] = match.Groups[group].Value;
            return variables;
        }
        catch (RegexMatchTimeoutException)
        {
            return null;
        }
    }

    private ParseResult<ChapterInfo> ParseChapterItems(IDocument document, Uri pageUrl)
    {
        ChaptersSpec spec = _definition.Chapters;
        List<INode> nodes = SelectorEvaluator.SelectAll(document, spec.ItemSelector, relative: false);
        List<ChapterInfo> chapters = [];
        int missing = 0;
        foreach (INode node in nodes)
        {
            string? numberText = spec.Number.IsUnused ? SelectorEvaluator.NormalizedText(node) : SelectorEvaluator.ExtractText(node, spec.Number);
            if (SelectorEvaluator.ExtractUrl(node, spec.Url, pageUrl) is not { } url || ParseChapterNumber(numberText) is not { } number)
            {
                missing++;
                continue;
            }

            chapters.Add(new ChapterInfo(Identifier, number, url, url, Volume: null,
                Title: SelectorEvaluator.ExtractText(node, spec.Title),
                ReleaseDate: ParseDate(SelectorEvaluator.ExtractText(node, spec.Date), spec.DateFormat)));
        }

        return new ParseResult<ChapterInfo>(chapters, nodes.Count, missing);
    }

    private Uri? ParseNextPage(IDocument document, Uri pageUrl) =>
        SelectorEvaluator.ExtractUrl(document, _definition.Chapters.NextPage, pageUrl) is { } next ? new Uri(next) : null;

    /// <summary>"Chapter 12.5" -> "12.5", "#012" -> "12", "Extra" -> null</summary>
    internal static string? ParseChapterNumber(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        string candidate = decimal.TryParse(text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out _)
            ? text.Trim()
            : FirstNumber.Match(text) is { Success: true } match ? match.Value : "";
        return decimal.TryParse(candidate, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal number) && number >= 0
            ? number.ToString(CultureInfo.InvariantCulture)
            : null;
    }

    internal static DateTimeOffset? ParseDate(string? text, string? format)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        const DateTimeStyles styles = DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowWhiteSpaces;
        bool parsed = string.IsNullOrWhiteSpace(format)
            ? DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, styles, out DateTimeOffset date)
            : DateTimeOffset.TryParseExact(text, format, CultureInfo.InvariantCulture, styles, out date);
        return parsed ? date : null;
    }

    #endregion

    #region Images

    /// <inheritdoc />
    public async Task<List<ChapterImage>?> FetchChapterImages(ChapterInfo chapterInfo, CancellationToken ct)
    {
        if (BuildUrl(_definition.Images.UrlTemplate, new Dictionary<string, string> { ["url"] = chapterInfo.Identifier }) is not { } url ||
            await TryFetchDocument(url, ct) is not { Document: { } document } page)
            return null;

        List<string>? imageUrls = await CollectImageUrls(document, page.Url, ct);
        if (imageUrls is not { Count: > 0 })
            return null;

        TrangaImage?[] downloaded = await Task.WhenAll(imageUrls.Select(imageUrl => DownloadImage(new Uri(imageUrl), chapterInfo.Identifier, ct)));

        // A chapter with a missing page is worse than no chapter, so any failed page fails the whole chapter.
        if (downloaded.Any(image => image is null))
            return null;

        return downloaded
            .Select((image, index) => new ChapterImage(Identifier, chapterInfo.Identifier, index, image!))
            .ToList();
    }

    /// <summary>
    /// The image URLs of a chapter. Normally they are all on the chapter page; with a
    /// <see cref="ImagesSpec.PageSelector"/> every page of the chapter is fetched, one after the other (the site's
    /// rate limit applies). Null when one of the pages could not be loaded or has no image, because a chapter with a
    /// missing page is worse than no chapter.
    /// </summary>
    private async Task<List<string>?> CollectImageUrls(IDocument chapterDocument, Uri chapterUrl, CancellationToken ct)
    {
        List<Uri> pageUrls = ParsePageUrls(chapterDocument, chapterUrl);
        if (pageUrls.Count == 0)
            return ParseImageUrls(chapterDocument, chapterUrl);

        List<string> imageUrls = [];
        foreach (Uri pageUrl in pageUrls)
        {
            // Usually the first option of the page list is the chapter page itself, which is already loaded.
            FetchedPage? page = pageUrl == chapterUrl ? new FetchedPage(chapterDocument, chapterUrl) : await TryFetchDocument(pageUrl, ct);
            if (page?.Document is not { } document)
                return null;

            List<string> found = ParseImageUrls(document, page.Url);
            if (found.Count == 0)
                return null;
            imageUrls.AddRange(found);
        }

        return imageUrls.Distinct().ToList();
    }

    /// <summary>The pages of a chapter that shows one image per page, in order. Empty when no page list is configured or found.</summary>
    private List<Uri> ParsePageUrls(IDocument document, Uri pageUrl)
    {
        SelectorField field = _definition.Images.PageSelector;
        if (string.IsNullOrWhiteSpace(field.Selector))
            return [];

        SelectorField link = field with { Selector = "", Attribute = string.IsNullOrWhiteSpace(field.Attribute) ? DefaultPageAttributes : field.Attribute };
        return SelectorEvaluator.SelectAll(document, field.Selector, relative: false)
            .Select(node => SelectorEvaluator.ExtractUrl(node, link, pageUrl))
            .OfType<string>()
            .Distinct()
            .Take(MaxChapterImagePages)
            .Select(url => new Uri(url))
            .ToList();
    }

    private List<string> ParseImageUrls(IDocument document, Uri pageUrl)
    {
        ImagesSpec spec = _definition.Images;
        List<INode> nodes = SelectorEvaluator.SelectAll(document, spec.ImageSelector, relative: false);
        SelectorField imageField = new() { Attribute = spec.Attribute ?? DefaultImageAttributes };
        return nodes
            .Select(node => SelectorEvaluator.ExtractUrl(node, imageField, pageUrl))
            .OfType<string>()
            .Distinct()
            .ToList();
    }

    private Task<TrangaImage?> DownloadImage(Uri url, string referer, CancellationToken ct) =>
        ImageDownloader.DownloadAsync(ClientFor(url), () => CreateRequest(url, referer), null, ct);

    #endregion

    #region Testing

    /// <summary>Runs the search step against the website and reports what was extracted.</summary>
    public Task<StepTestResult> TestSearchAsync(string query, CancellationToken ct) => TestStep(SearchUrl(query), ct, (document, pageUrl) =>
    {
        ParseResult<SearchItem> result = ParseSearchItems(document, pageUrl);
        return (result.Matched, result.Missing, result.Items.Count, result.Items.Select(i => new Dictionary<string, string?>
        {
            ["title"] = i.Title, ["url"] = i.Url, ["cover"] = i.CoverUrl
        }), "title, URL or cover");
    });

    /// <summary>Runs the chapter list step for a series URL and reports what was extracted.</summary>
    public Task<StepTestResult> TestChaptersAsync(string seriesUrl, CancellationToken ct)
    {
        if (ParseIdentifierFromUrl(seriesUrl) is not { } identifier || SeriesVariables(identifier) is not { } variables ||
            BuildUrl(_definition.Chapters.UrlTemplate, variables) is not { } url)
            return Task.FromResult(new StepTestResult { Error = "That URL is not a series page of this source (check the base URL and the series URL pattern)." });

        return TestStep(url, ct, (document, pageUrl) =>
        {
            ParseResult<ChapterInfo> result = ParseChapterItems(document, pageUrl);
            return (result.Matched, result.Missing, result.Items.Count, result.Items.Select(c => new Dictionary<string, string?>
            {
                ["number"] = c.Number, ["title"] = c.Title, ["url"] = c.Url, ["date"] = c.ReleaseDate?.ToString("u")
            }), "URL or chapter number");
        });
    }

    /// <summary>
    /// Runs the page image step for a chapter URL and reports what was extracted (without downloading the images).
    /// With a <see cref="ImagesSpec.PageSelector"/> the rows are the pages of the chapter; only the first few of them
    /// are loaded to check that the image selector finds an image (loading all of them could take minutes).
    /// </summary>
    public async Task<StepTestResult> TestImagesAsync(string chapterUrl, CancellationToken ct)
    {
        if (BuildUrl(_definition.Images.UrlTemplate, new Dictionary<string, string> { ["url"] = chapterUrl }) is not { } url)
            return new StepTestResult { Error = "The page URL could not be built from that chapter URL." };

        bool listsPages = false;
        StepTestResult result = await TestStep(url, ct, (document, pageUrl) =>
        {
            List<Uri> pages = ParsePageUrls(document, pageUrl);
            if (pages.Count > 0)
            {
                listsPages = true;
                return (pages.Count, 0, pages.Count, pages.Select(p => new Dictionary<string, string?> { ["page"] = p.AbsoluteUri }), "page link");
            }

            List<string> urls = ParseImageUrls(document, pageUrl);
            int matched = SelectorEvaluator.SelectAll(document, _definition.Images.ImageSelector, relative: false).Count;
            return (matched, matched - urls.Count, urls.Count, urls.Select(u => new Dictionary<string, string?> { ["url"] = u }), "usable image URL");
        });

        if (!listsPages || result.Error is not null)
            return result;

        List<string> warnings = [.. result.Warnings];
        foreach (Dictionary<string, string?> row in result.Rows.Take(MaxTestSampledPages))
        {
            Uri pageUrl = new(row["page"]!);
            if (await TryFetchDocument(pageUrl, ct) is not { Document: { } document } page)
            {
                warnings.Add($"The page {pageUrl} could not be loaded.");
                continue;
            }

            List<string> images = ParseImageUrls(document, page.Url);
            if (images.Count == 0)
                warnings.Add($"The image selector found no image on the page {pageUrl}.");
            else
                row["image"] = images[0];
        }

        if (result.ItemCount > MaxTestSampledPages)
            warnings.Add($"Only the first {MaxTestSampledPages} of {result.ItemCount} pages were loaded to look for images.");
        return result with { Warnings = warnings };
    }

    private async Task<StepTestResult> TestStep(Uri url, CancellationToken ct,
        Func<IDocument, Uri, (int Matched, int Missing, int Count, IEnumerable<Dictionary<string, string?>> Rows, string MissingWhat)> parse)
    {
        List<string> warnings = [];
        if (_definition.UseFlareSolverr && _options.FlareSolverrUrl is null)
            warnings.Add("FlareSolverr is enabled for this source, but no FlareSolverr instance is configured (FLARESOLVERR_URL). The request was sent directly.");

        try
        {
            using HttpRequestMessage request = CreateRequest(url, null);
            using HttpResponseMessage response = await ClientFor(url).SendAsync(request, ct);
            int status = (int)response.StatusCode;
            if (!response.IsSuccessStatusCode)
                return new StepTestResult
                {
                    RequestUrl = url.AbsoluteUri, StatusCode = status, Warnings = warnings,
                    Error = $"The website answered with HTTP {status} {response.ReasonPhrase}." + (status is 403 or 503 ? " It may be protected by Cloudflare: try enabling FlareSolverr." : "")
                };

            IDocument document = SelectorEvaluator.Parse(await response.Content.ReadAsStringAsync(ct));
            Uri pageUrl = response.RequestMessage?.RequestUri ?? url;
            (int matched, int missing, int count, IEnumerable<Dictionary<string, string?>> rows, string missingWhat) = parse(document, pageUrl);

            if (matched == 0)
                warnings.Add("The item selector matched no elements. The site may render this part with JavaScript, or the selector is wrong.");
            else if (missing > 0)
                warnings.Add($"{missing} of {matched} matched elements were skipped because they have no {missingWhat}.");

            return new StepTestResult
            {
                RequestUrl = url.AbsoluteUri, StatusCode = status, ItemCount = count, Rows = rows.Take(MaxTestRows).ToList(), Warnings = warnings
            };
        }
        catch (Exception e) when (!ct.IsCancellationRequested)
        {
            return new StepTestResult { RequestUrl = url.AbsoluteUri, Warnings = warnings, Error = e.InnerException?.Message ?? e.Message };
        }
    }

    #endregion

    #region Http

    private sealed record ParseResult<T>(List<T> Items, int Matched, int Missing);

    private sealed record FetchedPage(IDocument? Document, Uri Url);

    private RequestClient ClientFor(Uri url) =>
        url.Host.Equals(_baseUri.Host, StringComparison.OrdinalIgnoreCase) ? _siteClient : _assetClient;

    /// <summary>
    /// Many sites reject unknown clients, so sources look like a regular browser unless the definition sets its own
    /// <c>User-Agent</c> header.
    /// </summary>
    internal const string DefaultUserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36";

    private HttpRequestMessage CreateRequest(Uri url, string? referer)
    {
        HttpRequestMessage request = new(HttpMethod.Get, url);
        if (referer is not null)
            request.Headers.Referrer = Uri.TryCreate(referer, UriKind.Absolute, out Uri? referrer) ? referrer : null;

        string? userAgent = null;
        foreach ((string name, string value) in _definition.Headers)
        {
            if (name.Equals("User-Agent", StringComparison.OrdinalIgnoreCase))
                userAgent = value;
            else
                request.Headers.TryAddWithoutValidation(name, value);
        }

        request.Headers.UserAgent.Clear();
        if (userAgent is null || !request.Headers.UserAgent.TryParseAdd(userAgent))
        {
            request.Headers.UserAgent.Clear();
            request.Headers.UserAgent.ParseAdd(DefaultUserAgent);
        }

        return request;
    }

    /// <summary>Downloads and parses a page. Null when the request failed (blocked host, network error, non-success status).</summary>
    private async Task<FetchedPage?> TryFetchDocument(Uri url, CancellationToken ct)
    {
        try
        {
            using HttpRequestMessage request = CreateRequest(url, null);
            using HttpResponseMessage response = await ClientFor(url).SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                return null;
            IDocument document = SelectorEvaluator.Parse(await response.Content.ReadAsStringAsync(ct));
            return new FetchedPage(document, response.RequestMessage?.RequestUri ?? url);
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            return null;
        }
    }

    /// <summary>Fills in a URL template and resolves it against the base URL. Null if the result is not an http(s) URL.</summary>
    private Uri? BuildUrl(string template, IReadOnlyDictionary<string, string> variables)
    {
        string filled = Placeholder.Replace(template, match => variables.TryGetValue(match.Groups[1].Value, out string? value) ? value : match.Value);
        return Uri.TryCreate(_baseUri, filled, out Uri? url) && url.Scheme is "http" or "https" ? url : null;
    }

    #endregion
}
