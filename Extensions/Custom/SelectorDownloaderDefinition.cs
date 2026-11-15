using System.Text.RegularExpressions;
using Common.Datatypes;

namespace Extensions.Custom;

/// <summary>
/// Describes a user-defined download source that is driven entirely by CSS/XPath selectors, so that people can add a
/// website to Tranga without writing code. Turned into an <see cref="IDownloadExtension"/> by
/// <see cref="SelectorDownloadExtension"/>; persisted as one JSON document.
/// </summary>
public sealed record SelectorDownloaderDefinition
{
    /// <summary>Becomes <see cref="IExtension.Identifier"/>. Persisted on download links, so it must never change.</summary>
    public Guid Id { get; init; }

    /// <summary>Display name of the source.</summary>
    public string Name { get; init; } = "";

    /// <summary>Root of the website, e.g. <c>https://weebcentral.com</c>. Relative URLs are resolved against it.</summary>
    public string BaseUrl { get; init; } = "";

    /// <summary>Optional icon, defaults to the website's favicon.</summary>
    public string? IconUrl { get; init; }

    /// <summary>Languages the website publishes in (e.g. <c>en</c>). A search in another language finds nothing.</summary>
    public string[] Languages { get; init; } = ["en"];

    /// <summary>Route requests through the configured FlareSolverr instance (for Cloudflare protected sites).</summary>
    public bool UseFlareSolverr { get; init; }

    /// <summary>Requests per second to the website itself (0.1 - 20). Image CDNs on other hosts are limited separately.</summary>
    public double RequestsPerSecond { get; init; } = 1;

    /// <summary>Extra request headers, e.g. a cookie or a User-Agent.</summary>
    public Dictionary<string, string> Headers { get; init; } = new();

    /// <summary>
    /// Optional regular expression that a series URL has to match. Its named groups (e.g. <c>(?&lt;id&gt;[^/]+)</c>) are
    /// available as <c>{id}</c> in <see cref="ChaptersSpec.UrlTemplate"/> for sites whose chapter list lives at a
    /// different URL than the series page.
    /// </summary>
    public string? SeriesUrlPattern { get; init; }

    /// <summary>How to find series.</summary>
    public SearchSpec Search { get; init; } = new();

    /// <summary>How to list the chapters of a series.</summary>
    public ChaptersSpec Chapters { get; init; } = new();

    /// <summary>How to find the pages of a chapter.</summary>
    public ImagesSpec Images { get; init; } = new();

    internal static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    private static readonly string[] ForbiddenHeaders = ["host", "content-length", "transfer-encoding", "connection"];
    private static readonly Regex Placeholder = new(@"\{(\w+)\}", RegexOptions.Compiled, RegexTimeout);

    /// <summary>Checks the whole definition and returns a human readable message for everything that is wrong with it.</summary>
    public IReadOnlyList<string> Validate() => Validate(null);

    /// <summary>
    /// Checks the general settings and, when <paramref name="step"/> is given, only that section (so a single step can
    /// be tried while the rest of the form is still empty).
    /// </summary>
    public IReadOnlyList<string> Validate(SelectorDownloaderStep? step)
    {
        List<string> errors = [];

        if (Id == Guid.Empty)
            errors.Add("Id must be set.");
        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Name is required.");
        else if (Name.Length > 256)
            errors.Add("Name can be at most 256 characters long.");
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out Uri? baseUri) || baseUri.Scheme is not ("http" or "https"))
            errors.Add("Base URL must be an absolute http(s) URL.");
        if (IconUrl is { Length: > 0 } && !Uri.TryCreate(IconUrl, UriKind.Absolute, out _))
            errors.Add("Icon URL must be an absolute URL.");
        if (Languages is not { Length: > 0 })
            errors.Add("At least one language is required.");
        else
            foreach (string language in Languages.Where(l => !IsValidLanguage(l)))
                errors.Add($"'{language}' is not a valid language code.");
        if (RequestsPerSecond is < 0.1 or > 20 || double.IsNaN(RequestsPerSecond))
            errors.Add("Requests per second must be between 0.1 and 20.");
        foreach (string header in Headers.Keys)
        {
            if (string.IsNullOrWhiteSpace(header) || header.Any(c => c is ':' or ' ' or '\r' or '\n'))
                errors.Add($"'{header}' is not a valid header name.");
            else if (ForbiddenHeaders.Contains(header.ToLowerInvariant()))
                errors.Add($"The '{header}' header cannot be set.");
        }
        foreach (string value in Headers.Values.Where(v => v.Contains('\r') || v.Contains('\n')))
            errors.Add($"Header values cannot contain line breaks ('{value.ReplaceLineEndings(" ")}').");

        IReadOnlyCollection<string> seriesGroups = [];
        if (!string.IsNullOrWhiteSpace(SeriesUrlPattern))
        {
            try
            {
                Regex pattern = new(SeriesUrlPattern, RegexOptions.None, RegexTimeout);
                seriesGroups = pattern.GetGroupNames().Where(n => !int.TryParse(n, out _)).ToArray();
            }
            catch (ArgumentException e)
            {
                errors.Add($"Series URL pattern is not a valid regular expression: {e.Message}");
            }
        }

        if (step is null or SelectorDownloaderStep.Search)
        {
            ValidateTemplate(errors, "Search URL", Search.UrlTemplate, ["query", "page"], required: true);
            ValidateSelector(errors, "Search result item selector", Search.ItemSelector, required: true);
            ValidateField(errors, "Search result title", Search.Title, required: true);
            ValidateField(errors, "Search result URL", Search.Url, required: true);
            ValidateField(errors, "Search result cover", Search.Cover, required: true);
        }

        if (step is null or SelectorDownloaderStep.Chapters)
        {
            ValidateTemplate(errors, "Chapter list URL", Chapters.UrlTemplate, ["url", .. seriesGroups], required: true);
            ValidateSelector(errors, "Chapter item selector", Chapters.ItemSelector, required: true);
            ValidateField(errors, "Chapter URL", Chapters.Url, required: true);
            ValidateField(errors, "Chapter number", Chapters.Number, required: false);
            ValidateField(errors, "Chapter title", Chapters.Title, required: false);
            ValidateField(errors, "Chapter date", Chapters.Date, required: false);
            ValidateField(errors, "Chapter list next page", Chapters.NextPage, required: false);
        }

        if (step is null or SelectorDownloaderStep.Images)
        {
            ValidateTemplate(errors, "Chapter pages URL", Images.UrlTemplate, ["url"], required: true);
            ValidateSelector(errors, "Page image selector", Images.ImageSelector, required: true);
        }

        return errors;
    }

    private static bool IsValidLanguage(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return false;
        try
        {
            // Unknown names do not throw on every platform, they become "custom cultures" with this LCID.
            const int customCultureLcid = 0x1000;
            return new Language(language).LCID != customCultureLcid;
        }
        catch (System.Globalization.CultureNotFoundException)
        {
            return false;
        }
    }

    private static void ValidateTemplate(List<string> errors, string label, string template, IEnumerable<string> allowed, bool required)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            if (required)
                errors.Add($"{label} is required.");
            return;
        }

        string[] known = allowed.ToArray();
        foreach (Match match in Placeholder.Matches(template))
            if (!known.Contains(match.Groups[1].Value))
                errors.Add($"{label} uses unknown placeholder '{match.Value}' (available: {string.Join(", ", known.Select(k => "{" + k + "}"))}).");
    }

    private static void ValidateSelector(List<string> errors, string label, string selector, bool required)
    {
        if (string.IsNullOrWhiteSpace(selector))
        {
            if (required)
                errors.Add($"{label} is required.");
            return;
        }

        if (SelectorEvaluator.TryCompile(selector) is { } problem)
            errors.Add($"{label} is not a valid selector: {problem}");
    }

    private static void ValidateField(List<string> errors, string label, SelectorField field, bool required)
    {
        if (field.IsUnused)
        {
            if (required)
                errors.Add($"{label} is required.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(field.Selector) && SelectorEvaluator.TryCompile(field.Selector) is { } problem)
            errors.Add($"{label} is not a valid selector: {problem}");
        if (!string.IsNullOrEmpty(field.Regex))
        {
            try
            {
                _ = new Regex(field.Regex, RegexOptions.None, RegexTimeout);
            }
            catch (ArgumentException e)
            {
                errors.Add($"{label} regex is not valid: {e.Message}");
            }
        }
    }
}

/// <summary>
/// Extracts one value from an HTML element: <see cref="Selector"/> picks the element (relative to the item the field
/// belongs to; empty = the item itself), <see cref="Attribute"/> picks what to read (empty = the element's text), and
/// the optional <see cref="Regex"/> post-processes the result.
/// </summary>
public sealed record SelectorField
{
    /// <summary>
    /// CSS selector or XPath expression (XPath when it starts with <c>/</c>, <c>./</c>, <c>../</c> or <c>(</c>).
    /// An XPath may end in <c>/@attribute</c> or <c>/text()</c>.
    /// </summary>
    public string Selector { get; init; } = "";

    /// <summary>
    /// Attribute to read, e.g. <c>href</c>. A comma separated list (<c>srcset,src</c>) is tried in order and the first
    /// non-empty value wins. Empty reads the element's text.
    /// </summary>
    public string? Attribute { get; init; }

    /// <summary>
    /// Regular expression applied to the value. Without <see cref="Replacement"/> the first capture group (or the
    /// whole match) is kept; a value that does not match counts as missing.
    /// </summary>
    public string? Regex { get; init; }

    /// <summary>When set, <see cref="Regex"/> replaces every match with this text instead of extracting.</summary>
    public string? Replacement { get; init; }

    /// <summary>True when the field is not configured at all.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsUnused => string.IsNullOrWhiteSpace(Selector) && string.IsNullOrWhiteSpace(Attribute) && string.IsNullOrEmpty(Regex);
}

/// <summary>How to find series on the website.</summary>
public sealed record SearchSpec
{
    /// <summary>
    /// URL of the search results. <c>{query}</c> is replaced with the URL-encoded search text and <c>{page}</c> with the
    /// page number (always 1). Relative URLs are resolved against the base URL, e.g.
    /// <c>/search/data?text={query}&amp;limit=32</c>.
    /// </summary>
    public string UrlTemplate { get; init; } = "";

    /// <summary>Selects one element per search result (the same selector may also match the result's link).</summary>
    public string ItemSelector { get; init; } = "";

    /// <summary>The series title, relative to the result item.</summary>
    public SelectorField Title { get; init; } = new();

    /// <summary>Link to the series page, relative to the result item. Relative links are resolved.</summary>
    public SelectorField Url { get; init; } = new();

    /// <summary>The cover image URL, relative to the result item.</summary>
    public SelectorField Cover { get; init; } = new();
}

/// <summary>How to list the chapters of a series.</summary>
public sealed record ChaptersSpec
{
    /// <summary>
    /// URL of the page that lists the chapters. <c>{url}</c> is the series URL; the named groups of
    /// <see cref="SelectorDownloaderDefinition.SeriesUrlPattern"/> are available too, e.g.
    /// <c>https://example.com/series/{id}/chapters</c>.
    /// </summary>
    public string UrlTemplate { get; init; } = "{url}";

    /// <summary>Selects one element per chapter.</summary>
    public string ItemSelector { get; init; } = "";

    /// <summary>The chapter's link, relative to the chapter item.</summary>
    public SelectorField Url { get; init; } = new();

    /// <summary>
    /// The chapter number, relative to the chapter item. When unused the item's text is searched. The first number
    /// in the extracted text is used (<c>Chapter 12.5</c> becomes <c>12.5</c>); chapters without one are skipped.
    /// </summary>
    public SelectorField Number { get; init; } = new();

    /// <summary>Optional chapter title.</summary>
    public SelectorField Title { get; init; } = new();

    /// <summary>Optional release date.</summary>
    public SelectorField Date { get; init; } = new();

    /// <summary>Optional .NET date format for <see cref="Date"/> (e.g. <c>yyyy-MM-dd</c>); ISO 8601 and common formats are tried without it.</summary>
    public string? DateFormat { get; init; }

    /// <summary>Optional link to the next page of the chapter list (followed up to 50 times).</summary>
    public SelectorField NextPage { get; init; } = new();
}

/// <summary>How to find the pages of a chapter.</summary>
public sealed record ImagesSpec
{
    /// <summary>URL of the page that shows the images. <c>{url}</c> is the chapter URL, e.g. <c>{url}/images</c>.</summary>
    public string UrlTemplate { get; init; } = "{url}";

    /// <summary>Selects one element per page image, in reading order.</summary>
    public string ImageSelector { get; init; } = "";

    /// <summary>Attribute holding the image URL. Defaults to <c>data-src,data-lazy-src,src</c>.</summary>
    public string? Attribute { get; init; }
}
