using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.RateLimiting;
using Common.Datatypes;
using Common.Helpers;
using Common.Settings;
using Extensions.Data;
using Extensions.Helpers;
using FlareSolverrSharp;

namespace Extensions.Custom;

/// <summary>Environment dependent behaviour of <see cref="SelectorDownloadExtension"/>, injectable for tests.</summary>
/// <param name="AllowPrivateHosts">Whether sources may talk to loopback/LAN addresses</param>
/// <param name="FlareSolverrUrl">The FlareSolverr instance sources with <see cref="SelectorDownloaderDefinition.UseFlareSolverr"/> use, if any</param>
public sealed record SelectorDownloaderOptions(bool AllowPrivateHosts, string? FlareSolverrUrl);

/// <summary>
/// A download extension that reads a website with the selectors of a <see cref="SelectorDownloaderDefinition"/>:
/// CSS/XPath on HTML pages, or paths on the JSON of an API (<see cref="SelectorDownloaderSettings.Format"/>).
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

    // A placeholder inside the replacement of a field. "${name}" is a group of the field's own regex, not a placeholder.
    private static readonly Regex ReplacementPlaceholder = new(@"(?<!\$)\{(\w+)\}", RegexOptions.Compiled, SelectorDownloaderDefinition.RegexTimeout);

    private readonly SelectorDownloaderDefinition _definition;
    private readonly SelectorDownloaderOptions _options;
    private readonly Uri _baseUri;
    private readonly Regex? _seriesPattern;
    private readonly Regex? _chapterPattern;
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

    /// <param name="definition">A complete, valid definition with an <see cref="SelectorDownloaderSettings.Id"/></param>
    /// <param name="options">Defaults to the environment (<c>ALLOW_PRIVATE_SOURCE_HOSTS</c>, <c>FLARESOLVERR_URL</c>)</param>
    /// <exception cref="ArgumentException">The definition is invalid</exception>
    public SelectorDownloadExtension(SelectorDownloaderDefinition definition, SelectorDownloaderOptions? options = null)
        : this(Checked(definition), options, definitionChecked: true)
    {
    }

    /// <summary>
    /// Creates an extension for trying out a single step. It only needs the general <paramref name="settings"/> and the
    /// section of the step that is tried (the others stay empty and are never used), so a step can be tested while the
    /// rest of a definition does not exist yet. The settings and the given sections are validated; the
    /// <see cref="SelectorDownloaderSettings.Id"/> is assigned when missing. Used by the live test of the frontend.
    /// </summary>
    /// <param name="settings">General settings of the website. Pass the plain settings, not a whole definition</param>
    /// <param name="search">Needed to test the search</param>
    /// <param name="chapters">Needed to test the chapter list</param>
    /// <param name="images">Needed to test the pages of a chapter</param>
    /// <param name="options">Defaults to the environment</param>
    /// <exception cref="ArgumentException">The settings or one of the given sections is invalid</exception>
    public static SelectorDownloadExtension CreateForTest(SelectorDownloaderSettings settings, SearchSpec? search = null,
        ChaptersSpec? chapters = null, ImagesSpec? images = null, SelectorDownloaderOptions? options = null)
    {
        SelectorDownloaderSettings normalized = settings.Normalized();
        List<string> errors =
        [
            .. normalized.Validate(),
            .. (search?.Validate(nameof(SelectorDownloaderDefinition.Search), normalized.Format) ?? []).Select(r => r.ErrorMessage ?? ""),
            .. (chapters?.Validate(nameof(SelectorDownloaderDefinition.Chapters), normalized.Format, normalized.SeriesUrlGroups()) ?? []).Select(r => r.ErrorMessage ?? ""),
            .. (images?.Validate(nameof(SelectorDownloaderDefinition.Images), normalized.Format) ?? []).Select(r => r.ErrorMessage ?? "")
        ];
        if (errors.Count > 0)
            throw new ArgumentException(string.Join(" ", errors), nameof(settings));

        SelectorDownloaderDefinition definition = new SelectorDownloaderDefinition(normalized)
        {
            Id = normalized.Id == Guid.Empty ? Guid.NewGuid() : normalized.Id,
            Search = search ?? new SearchSpec(),
            Chapters = chapters ?? new ChaptersSpec(),
            Images = images ?? new ImagesSpec()
        };
        return new SelectorDownloadExtension(definition, options, definitionChecked: true);
    }

    /// <summary>The normalized definition, or an <see cref="ArgumentException"/> listing what is wrong with it.</summary>
    private static SelectorDownloaderDefinition Checked(SelectorDownloaderDefinition definition)
    {
        // Normalizing also covers definitions stored before the base URL was normalized when they were saved.
        SelectorDownloaderDefinition normalized = definition.Normalized();
        List<string> errors = [.. normalized.Validate()];
        // The id is not part of what a client sends (the server assigns it), but an extension cannot exist without one.
        if (normalized.Id == Guid.Empty)
            errors.Insert(0, "Id must be set.");
        if (errors.Count > 0)
            throw new ArgumentException(string.Join(" ", errors), nameof(definition));
        return normalized;
    }

    // definitionChecked only tells this constructor from the public one: the definition is valid for what it is used for.
    private SelectorDownloadExtension(SelectorDownloaderDefinition definition, SelectorDownloaderOptions? options, bool definitionChecked)
    {
        _definition = definition;
        // Without options they come from the environment (ALLOW_PRIVATE_SOURCE_HOSTS, FLARESOLVERR_URL).
        _options = options ?? new SelectorDownloaderOptions(EnvVars.AllowPrivateSourceHosts, EnvVars.FlareSolverrUrl);
        _baseUri = new Uri(definition.BaseUrl);
        _seriesPattern = string.IsNullOrWhiteSpace(definition.SeriesUrlPattern)
            ? null
            : new Regex(definition.SeriesUrlPattern, RegexOptions.None, SelectorDownloaderDefinition.RegexTimeout);
        _chapterPattern = string.IsNullOrWhiteSpace(definition.Images.UrlPattern)
            ? null
            : new Regex(definition.Images.UrlPattern, RegexOptions.None, SelectorDownloaderDefinition.RegexTimeout);

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
        // A website only has series in its own languages, so a search in another one finds nothing.
        Language language = query.Language ?? Settings.DownloadLanguage;
        if (!_definition.Languages.Any(language.Equals) || string.IsNullOrWhiteSpace(query.Title))
            return [];

        Dictionary<string, string> variables = SearchVariables(query.Title);
        if (await TryFetchPage(BuildUrl(_definition.Search.UrlTemplate, variables)!, ct) is not { Root: { } root } page)
            return null;

        List<SearchItem> items = ParseSearchItems(root, page.Url, variables).Items.Take(MaxSearchResults).ToList();
        MangaInfo?[] results = await Task.WhenAll(items.Select(async item =>
        {
            TrangaImage? cover = await DownloadImage(new Uri(item.CoverUrl), _baseUri.ToString(), ct);
            // MANGA sites do not tell us the content rating, so nothing is flagged NSFW.
            return cover is null ? null : new MangaInfo(Identifier, item.Title, item.Url, item.Url, cover, Description: null, NSFW: false);
        }));
        return results.OfType<MangaInfo>().ToList();
    }

    /// <summary>The placeholders of the search URL template: the URL-encoded search text and the page (always the first).</summary>
    private static Dictionary<string, string> SearchVariables(string title) =>
        new() { ["query"] = Uri.EscapeDataString(title.Trim()), ["page"] = "1" };

    private sealed record SearchItem(string Title, string Url, string CoverUrl);

    private ParseResult<SearchItem> ParseSearchItems(ISourceNode root, Uri pageUrl, IReadOnlyDictionary<string, string> variables)
    {
        SearchSpec spec = _definition.Search;
        SelectorField titleField = Fill(spec.Title, variables), urlField = Fill(spec.Url, variables), coverField = Fill(spec.Cover, variables);
        List<ISourceNode> nodes = root.Select(spec.ItemSelector);
        List<SearchItem> items = [];
        int missing = 0;
        foreach (ISourceNode node in nodes)
        {
            if (node.Text(titleField) is not { } title ||
                node.Url(urlField, pageUrl) is not { } url ||
                node.Url(coverField, pageUrl) is not { } cover)
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
        // The placeholders of the chapter list URL: {url} and the groups of the series URL pattern. A URL that does not
        // match the pattern is not a series of this source.
        if (UrlVariables(mangaInfo.Identifier, _seriesPattern) is not { } variables ||
            BuildUrl(_definition.Chapters.UrlTemplate, variables) is not { } firstUrl)
            return null;

        List<ChapterInfo> chapters = [];
        HashSet<string> visited = [];
        Uri? next = firstUrl;
        for (int pages = 0; next is not null && pages < MaxChapterPages && visited.Add(next.ToString()); pages++)
        {
            if (await TryFetchPage(next, ct) is not { Root: { } root } page)
                return pages == 0 ? null : chapters;

            chapters.AddRange(ParseChapterItems(root, page.Url, variables).Items);
            // The chapter list may go on on another page.
            next = root.Url(Fill(_definition.Chapters.NextPage, variables), page.Url) is { } nextUrl ? new Uri(nextUrl) : null;
        }

        return chapters.DistinctBy(c => c.Url).ToList();
    }

    /// <summary>
    /// The placeholders a URL provides for the URL template of the next step: <c>{url}</c> itself plus the named groups of
    /// <paramref name="pattern"/> (the series URL pattern for the chapter list, the chapter URL pattern for the pages).
    /// Null when the URL does not match the pattern.
    /// </summary>
    private static Dictionary<string, string>? UrlVariables(string url, Regex? pattern)
    {
        Dictionary<string, string> variables = new() { ["url"] = url };
        if (pattern is null)
            return variables;

        try
        {
            if (pattern.Match(url) is not { Success: true } match)
                return null;
            foreach (string group in pattern.GetGroupNames().Where(n => !int.TryParse(n, out _)))
                variables[group] = match.Groups[group].Value;
            return variables;
        }
        catch (RegexMatchTimeoutException)
        {
            return null;
        }
    }

    /// <summary>
    /// The field with the placeholders of its step filled into its replacement, so that a link can be built from a value
    /// that is only part of it (an id): <c>/series/{slug}/chapter/$1</c>.
    /// </summary>
    private static SelectorField Fill(SelectorField field, IReadOnlyDictionary<string, string> variables) =>
        string.IsNullOrEmpty(field.Replacement)
            ? field
            : field with
            {
                // "$" has a meaning in a replacement, so the ones of a value are escaped.
                Replacement = ReplacementPlaceholder.Replace(field.Replacement,
                    match => variables.TryGetValue(match.Groups[1].Value, out string? value) ? value.Replace("$", "$$") : match.Value)
            };

    private ParseResult<ChapterInfo> ParseChapterItems(ISourceNode root, Uri pageUrl, IReadOnlyDictionary<string, string> variables)
    {
        ChaptersSpec spec = _definition.Chapters;
        SelectorField urlField = Fill(spec.Url, variables), numberField = Fill(spec.Number, variables);
        SelectorField titleField = Fill(spec.Title, variables), dateField = Fill(spec.Date, variables);
        bool numberFromItem = SelectorValidation.IsUnused(spec.Number, _definition.Format);
        List<ISourceNode> nodes = root.Select(spec.ItemSelector);
        List<ChapterInfo> chapters = [];
        int missing = 0;
        foreach (ISourceNode node in nodes)
        {
            string? numberText = numberFromItem ? node.OwnText : node.Text(numberField);
            if (node.Url(urlField, pageUrl) is not { } url || ParseChapterNumber(numberText) is not { } number)
            {
                missing++;
                continue;
            }

            chapters.Add(new ChapterInfo(Identifier, number, url, url, Volume: null,
                Title: node.Text(titleField),
                ReleaseDate: ParseDate(node.Text(dateField), spec.DateFormat)));
        }

        return new ParseResult<ChapterInfo>(chapters, nodes.Count, missing);
    }

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
        // The placeholders of the pages URL: {url} and the groups of the chapter URL pattern.
        if (UrlVariables(chapterInfo.Identifier, _chapterPattern) is not { } variables ||
            BuildUrl(_definition.Images.UrlTemplate, variables) is not { } url ||
            await TryFetchPage(url, ct) is not { Root: { } root } page)
            return null;

        List<string>? imageUrls = await CollectImageUrls(root, page.Url, variables, ct);
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
    private async Task<List<string>?> CollectImageUrls(ISourceNode chapterRoot, Uri chapterUrl, IReadOnlyDictionary<string, string> variables, CancellationToken ct)
    {
        List<Uri> pageUrls = ParsePageUrls(chapterRoot, chapterUrl, variables);
        if (pageUrls.Count == 0)
            return ParseImageUrls(chapterRoot, chapterUrl);

        List<string> imageUrls = [];
        foreach (Uri pageUrl in pageUrls)
        {
            // Usually the first option of the page list is the chapter page itself, which is already loaded.
            FetchedPage? page = pageUrl == chapterUrl ? new FetchedPage(chapterRoot, chapterUrl) : await TryFetchPage(pageUrl, ct);
            if (page?.Root is not { } root)
                return null;

            List<string> found = ParseImageUrls(root, page.Url);
            if (found.Count == 0)
                return null;
            imageUrls.AddRange(found);
        }

        return imageUrls.Distinct().ToList();
    }

    /// <summary>The pages of a chapter that shows one image per page, in order. Empty when no page list is configured or found.</summary>
    private List<Uri> ParsePageUrls(ISourceNode root, Uri pageUrl, IReadOnlyDictionary<string, string> variables)
    {
        SelectorField field = Fill(_definition.Images.PageSelector, variables);
        if (string.IsNullOrWhiteSpace(field.Selector))
            return [];

        return root.Select(field.Selector)
            .Select(node => node.OwnUrl(field, DefaultPageAttributes, pageUrl))
            .OfType<string>()
            .Distinct()
            .Take(MaxChapterImagePages)
            .Select(url => new Uri(url))
            .ToList();
    }

    private List<string> ParseImageUrls(ISourceNode root, Uri pageUrl)
    {
        ImagesSpec spec = _definition.Images;
        SelectorField imageField = new() { Attribute = spec.Attribute };
        return root.Select(spec.ImageSelector)
            .Select(node => node.OwnUrl(imageField, DefaultImageAttributes, pageUrl))
            .OfType<string>()
            .Distinct()
            .ToList();
    }

    private Task<TrangaImage?> DownloadImage(Uri url, string referer, CancellationToken ct) =>
        ImageDownloader.DownloadAsync(ClientFor(url), () => CreateRequest(url, referer), null, ct);

    #endregion

    #region Testing

    /// <summary>Runs the search step against the website and reports what was extracted.</summary>
    public Task<StepTestResult> TestSearchAsync(string query, CancellationToken ct)
    {
        Dictionary<string, string> variables = SearchVariables(query);
        return TestStep(BuildUrl(_definition.Search.UrlTemplate, variables)!, ct, (root, pageUrl) =>
        {
            ParseResult<SearchItem> result = ParseSearchItems(root, pageUrl, variables);
            return (result.Matched, result.Missing, result.Items.Count, result.Items.Select(i => new Dictionary<string, string?>
            {
                ["title"] = i.Title, ["url"] = i.Url, ["cover"] = i.CoverUrl
            }), "title, URL or cover");
        });
    }

    /// <summary>Runs the chapter list step for a series URL and reports what was extracted.</summary>
    public Task<StepTestResult> TestChaptersAsync(string seriesUrl, CancellationToken ct)
    {
        if (ParseIdentifierFromUrl(seriesUrl) is not { } identifier || UrlVariables(identifier, _seriesPattern) is not { } variables ||
            BuildUrl(_definition.Chapters.UrlTemplate, variables) is not { } url)
            return Task.FromResult(new StepTestResult { Error = "That URL is not a series page of this source (check the base URL and the series URL pattern)." });

        return TestStep(url, ct, (root, pageUrl) =>
        {
            ParseResult<ChapterInfo> result = ParseChapterItems(root, pageUrl, variables);
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
        if (UrlVariables(chapterUrl.Trim(), _chapterPattern) is not { } variables)
            return new StepTestResult { Error = "That URL does not match the chapter URL pattern." };
        if (BuildUrl(_definition.Images.UrlTemplate, variables) is not { } url)
            return new StepTestResult { Error = "The page URL could not be built from that chapter URL." };

        bool listsPages = false;
        StepTestResult result = await TestStep(url, ct, (root, pageUrl) =>
        {
            List<Uri> pages = ParsePageUrls(root, pageUrl, variables);
            if (pages.Count > 0)
            {
                listsPages = true;
                return (pages.Count, 0, pages.Count, pages.Select(p => new Dictionary<string, string?> { ["page"] = p.AbsoluteUri }), "page link");
            }

            List<string> urls = ParseImageUrls(root, pageUrl);
            int matched = root.Select(_definition.Images.ImageSelector).Count;
            return (matched, matched - urls.Count, urls.Count, urls.Select(u => new Dictionary<string, string?> { ["url"] = u }), "usable image URL");
        });

        if (!listsPages || result.Error is not null)
            return result;

        List<string> warnings = [.. result.Warnings];
        foreach (Dictionary<string, string?> row in result.Rows.Take(MaxTestSampledPages))
        {
            Uri pageUrl = new(row["page"]!);
            if (await TryFetchPage(pageUrl, ct) is not { Root: { } root } page)
            {
                warnings.Add($"The page {pageUrl} could not be loaded.");
                continue;
            }

            List<string> images = ParseImageUrls(root, page.Url);
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
        Func<ISourceNode, Uri, (int Matched, int Missing, int Count, IEnumerable<Dictionary<string, string?>> Rows, string MissingWhat)> parse)
    {
        List<string> warnings = [];
        if (_definition.UseFlareSolverr && _options.FlareSolverrUrl is null)
            warnings.Add("FlareSolverr is enabled for this source, but no FlareSolverr instance is configured (FLARESOLVERR_URL). The request was sent directly.");

        try
        {
            bool json = _definition.Format == SourceFormat.Json;
            using HttpRequestMessage request = CreateRequest(url, null, api: json);
            using HttpResponseMessage response = await ClientFor(url).SendAsync(request, ct);
            int status = (int)response.StatusCode;
            if (!response.IsSuccessStatusCode)
                return new StepTestResult
                {
                    RequestUrl = url.AbsoluteUri, StatusCode = status, Warnings = warnings,
                    Error = $"The website answered with HTTP {status} {response.ReasonPhrase}." + (status is 403 or 503 ? " It may be protected by Cloudflare: try enabling FlareSolverr." : "")
                };

            string body = await response.Content.ReadAsStringAsync(ct);
            ISourceNode root;
            try
            {
                root = SourceContent.Parse(body, _definition.Format);
            }
            catch (JsonException)
            {
                return new StepTestResult
                {
                    RequestUrl = url.AbsoluteUri, StatusCode = status, Warnings = warnings,
                    Error = "The website did not answer with JSON. " + (body.TrimStart().StartsWith('<')
                        ? "It answered with an HTML page: use the address the website loads its data from (the browser's network tab shows it), or set the response format of the source to HTML."
                        : "Check the URL.")
                };
            }

            Uri pageUrl = response.RequestMessage?.RequestUri ?? url;
            (int matched, int missing, int count, IEnumerable<Dictionary<string, string?>> rows, string missingWhat) = parse(root, pageUrl);

            if (matched == 0 && json)
                warnings.Add("The item path matched nothing in the answer of the website. Check the path against the JSON it returned.");
            else if (matched == 0 && body.TrimStart() is ['{' or '[', ..])
                warnings.Add("The item selector matched no elements, and the website answered with JSON, not HTML: set the response format of the source to JSON.");
            else if (matched == 0)
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

    private sealed record FetchedPage(ISourceNode? Root, Uri Url);

    private RequestClient ClientFor(Uri url) =>
        url.Host.Equals(_baseUri.Host, StringComparison.OrdinalIgnoreCase) ? _siteClient : _assetClient;

    /// <summary>
    /// Many sites reject unknown clients, so sources look like a regular browser unless the definition sets its own
    /// <c>User-Agent</c> header.
    /// </summary>
    internal const string DefaultUserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36";

    /// <param name="url">What to request</param>
    /// <param name="referer">Sent as <c>Referer</c>, when given</param>
    /// <param name="api">The request goes to a JSON API: asks for JSON, unless the definition sets its own <c>Accept</c> header</param>
    private HttpRequestMessage CreateRequest(Uri url, string? referer, bool api = false)
    {
        HttpRequestMessage request = new(HttpMethod.Get, url);
        if (referer is not null)
            request.Headers.Referrer = Uri.TryCreate(referer, UriKind.Absolute, out Uri? referrer) ? referrer : null;
        if (api && !_definition.Headers.Keys.Any(name => name.Equals("Accept", StringComparison.OrdinalIgnoreCase)))
            request.Headers.TryAddWithoutValidation("Accept", "application/json, text/plain, */*");

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

    /// <summary>
    /// Downloads and parses a page (HTML) or an answer of the API (JSON). Null when the request failed (blocked host,
    /// network error, non-success status) or the answer is not in the format of the source.
    /// </summary>
    private async Task<FetchedPage?> TryFetchPage(Uri url, CancellationToken ct)
    {
        try
        {
            using HttpRequestMessage request = CreateRequest(url, null, api: _definition.Format == SourceFormat.Json);
            using HttpResponseMessage response = await ClientFor(url).SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                return null;
            ISourceNode root = SourceContent.Parse(await response.Content.ReadAsStringAsync(ct), _definition.Format);
            return new FetchedPage(root, response.RequestMessage?.RequestUri ?? url);
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
