using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Extensions.Custom;

/// <summary>What a website answers with, which decides how its responses are read.</summary>
/// <remarks>Written and read as <c>"html"</c> / <c>"json"</c>, in exactly this spelling.</remarks>
[JsonConverter(typeof(SourceFormatJsonConverter))]
public enum SourceFormat
{
    /// <summary>HTML pages, read with CSS selectors or XPath expressions.</summary>
    [JsonStringEnumMemberName("html")]
    Html,

    /// <summary>A JSON API, read with paths like <c>data[*].title</c>.</summary>
    [JsonStringEnumMemberName("json")]
    Json
}

/// <summary>Reads and writes a <see cref="SourceFormat"/> as text, never as a number.</summary>
public sealed class SourceFormatJsonConverter() : JsonStringEnumConverter<SourceFormat>(namingPolicy: null, allowIntegerValues: false);

/// <summary>
/// The general settings of a user-defined download source (the website itself, not how to scrape it). They validate
/// themselves (<see cref="IValidatableObject"/>), so whoever receives one - an API endpoint, the test of a single step,
/// the engine - does not have to know the rules.
/// </summary>
public record SelectorDownloaderSettings : IValidatableObject
{
    /// <summary>Becomes <see cref="IExtension.Identifier"/>. Persisted on download links, so it must never change. Assigned by the server.</summary>
    public Guid Id { get; init; }

    /// <summary>Display name of the source.</summary>
    public string Name { get; init; } = "";

    /// <summary>Root of the website, e.g. <c>https://weebcentral.com</c>. Relative URLs are resolved against it.</summary>
    public string BaseUrl { get; init; } = "";

    /// <summary>Optional icon, defaults to the website's favicon.</summary>
    public string? IconUrl { get; init; }

    /// <summary>
    /// What the website answers with: HTML pages (the default) or JSON, for websites that load their content from an API.
    /// It applies to every step and decides what a "selector" is: a CSS selector / XPath expression, or a JSON path.
    /// </summary>
    public SourceFormat Format { get; init; } = SourceFormat.Html;

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

    internal static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    private static readonly string[] ForbiddenHeaders = ["host", "content-length", "transfer-encoding", "connection"];

    /// <summary>
    /// The settings the way they are stored and used: the base URL without surrounding whitespace and trailing slashes
    /// (<c>https://example.com/</c> is the same website as <c>https://example.com</c>, and relative URLs resolve the same).
    /// </summary>
    public SelectorDownloaderSettings Normalized() => this with { BaseUrl = NormalizeBaseUrl(BaseUrl) };

    internal static string NormalizeBaseUrl(string? baseUrl) => (baseUrl ?? "").Trim().TrimEnd('/');

    /// <summary>The names of the groups of <see cref="SeriesUrlPattern"/> (usable as <c>{name}</c> in URL templates). Empty without a valid pattern.</summary>
    public IReadOnlyCollection<string> SeriesUrlGroups() => SelectorValidation.GroupNames(SeriesUrlPattern);

    /// <summary>The messages of everything that is wrong with this object (empty = valid). Same rules as <see cref="Validate(ValidationContext)"/>.</summary>
    public IReadOnlyList<string> Validate() => Validate(new ValidationContext(this)).Select(r => r.ErrorMessage ?? "").ToList();

    /// <inheritdoc />
    public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(Name))
            yield return new ValidationResult("Name is required.", [nameof(Name)]);
        else if (Name.Length > 256)
            yield return new ValidationResult("Name can be at most 256 characters long.", [nameof(Name)]);

        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out Uri? baseUri) || baseUri.Scheme is not ("http" or "https"))
            yield return new ValidationResult("Base URL must be an absolute http(s) URL.", [nameof(BaseUrl)]);
        if (IconUrl is { Length: > 0 } && !Uri.TryCreate(IconUrl, UriKind.Absolute, out _))
            yield return new ValidationResult("Icon URL must be an absolute URL.", [nameof(IconUrl)]);

        if (Languages is not { Length: > 0 })
            yield return new ValidationResult("At least one language is required.", [nameof(Languages)]);
        else
            foreach (string language in Languages.Where(l => !SelectorValidation.IsValidLanguage(l)))
                yield return new ValidationResult($"'{language}' is not a valid language code.", [nameof(Languages)]);

        if (RequestsPerSecond is < 0.1 or > 20 || double.IsNaN(RequestsPerSecond))
            yield return new ValidationResult("Requests per second must be between 0.1 and 20.", [nameof(RequestsPerSecond)]);

        foreach (string header in Headers.Keys)
        {
            if (string.IsNullOrWhiteSpace(header) || header.Any(c => c is ':' or ' ' or '\r' or '\n'))
                yield return new ValidationResult($"'{header}' is not a valid header name.", [nameof(Headers)]);
            else if (ForbiddenHeaders.Contains(header.ToLowerInvariant()))
                yield return new ValidationResult($"The '{header}' header cannot be set.", [nameof(Headers)]);
        }
        foreach (string value in Headers.Values.Where(v => v.Contains('\r') || v.Contains('\n')))
            yield return new ValidationResult($"Header values cannot contain line breaks ('{value.ReplaceLineEndings(" ")}').", [nameof(Headers)]);

        if (!Enum.IsDefined(Format))
            yield return new ValidationResult("Format must be html or json.", [nameof(Format)]);

        if (SelectorValidation.PatternProblem(SeriesUrlPattern) is { } problem)
            yield return new ValidationResult($"Series URL pattern is not a valid regular expression: {problem}", [nameof(SeriesUrlPattern)]);
    }
}

/// <summary>
/// Describes a user-defined download source that is driven entirely by CSS/XPath selectors, so that people can add a
/// website to Tranga without writing code: the <see cref="SelectorDownloaderSettings"/> of the website plus how to
/// search, list chapters and find the pages. Turned into an <see cref="IDownloadExtension"/> by
/// <see cref="SelectorDownloadExtension"/>; persisted as one JSON document. A definition is always validated as a whole;
/// to try a single step without the others, use the settings and that step's section on their own.
/// </summary>
public sealed record SelectorDownloaderDefinition : SelectorDownloaderSettings
{
    /// <summary>An empty definition (what the JSON deserializer starts from).</summary>
    public SelectorDownloaderDefinition()
    {
    }

    /// <summary>A definition with the given general settings and empty sections.</summary>
    public SelectorDownloaderDefinition(SelectorDownloaderSettings settings) : base(settings)
    {
    }

    /// <summary>How to find series.</summary>
    public SearchSpec Search { get; init; } = new();

    /// <summary>How to read the page of a series, for a series that was added by its URL. Optional.</summary>
    public SeriesSpec Series { get; init; } = new();

    /// <summary>How to list the chapters of a series.</summary>
    public ChaptersSpec Chapters { get; init; } = new();

    /// <summary>How to find the pages of a chapter.</summary>
    public ImagesSpec Images { get; init; } = new();

    /// <summary>The definition the way it is stored and used, see <see cref="SelectorDownloaderSettings.Normalized"/>.</summary>
    public new SelectorDownloaderDefinition Normalized() => this with { BaseUrl = NormalizeBaseUrl(BaseUrl) };

    /// <inheritdoc />
    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext) =>
        base.Validate(validationContext)
            .Concat(Search.Validate(nameof(Search), Format))
            .Concat(Series.Validate(nameof(Series), Format, SeriesUrlGroups()))
            .Concat(Chapters.Validate(nameof(Chapters), Format, SeriesUrlGroups()))
            .Concat(Images.Validate(nameof(Images), Format));
}

/// <summary>The rules that several parts of a definition have in common.</summary>
internal static class SelectorValidation
{
    private static readonly Regex Placeholder = new(@"\{(\w+)\}", RegexOptions.Compiled, SelectorDownloaderSettings.RegexTimeout);

    private static readonly Regex LanguageCode = new(@"^[a-z]{2,3}(-[a-z]{4})?(-([a-z]{2}|\d{3}))?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase, SelectorDownloaderSettings.RegexTimeout);

    public static bool IsValidLanguage(string language)
    {
        // Only codes like en, en-us or pt-BR: other words would otherwise become "custom cultures" on some platforms.
        if (string.IsNullOrWhiteSpace(language) || !LanguageCode.IsMatch(language))
            return false;
        try
        {
            // Unknown names do not throw on every platform, they become "custom cultures" with this LCID.
            const int customCultureLcid = 0x1000;
            return new Common.Datatypes.Language(language).LCID != customCultureLcid;
        }
        catch (System.Globalization.CultureNotFoundException)
        {
            return false;
        }
    }

    /// <summary>The names of the named groups of a regular expression (usable as <c>{name}</c> in URL templates). Empty without a valid pattern.</summary>
    public static IReadOnlyCollection<string> GroupNames(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return [];
        try
        {
            return new Regex(pattern, RegexOptions.None, SelectorDownloaderSettings.RegexTimeout).GetGroupNames().Where(n => !int.TryParse(n, out _)).ToArray();
        }
        catch (ArgumentException)
        {
            return [];
        }
    }

    /// <summary>Why an (optional) regular expression cannot be used. Null when it is empty or fine.</summary>
    public static string? PatternProblem(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            return null;
        try
        {
            _ = new Regex(pattern, RegexOptions.None, SelectorDownloaderSettings.RegexTimeout);
            return null;
        }
        catch (ArgumentException e)
        {
            return e.Message;
        }
    }

    /// <summary>
    /// True when a field is not configured for the format. JSON has no attributes, so there a field with only an
    /// attribute reads nothing.
    /// </summary>
    public static bool IsUnused(SelectorField field, SourceFormat format) => format == SourceFormat.Json
        ? string.IsNullOrWhiteSpace(field.Selector) && string.IsNullOrEmpty(field.Regex)
        : field.IsUnused;

    private static string SelectorWord(SourceFormat format) => format == SourceFormat.Json ? "JSON path" : "selector";

    public static IEnumerable<ValidationResult> Template(string member, string label, string template, IEnumerable<string> allowed, bool required)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            if (required)
                yield return new ValidationResult($"{label} is required.", [member]);
            yield break;
        }

        string[] known = allowed.ToArray();
        foreach (Match match in Placeholder.Matches(template))
            if (!known.Contains(match.Groups[1].Value))
                yield return new ValidationResult(
                    $"{label} uses unknown placeholder '{match.Value}' (available: {string.Join(", ", known.Select(k => "{" + k + "}"))}).", [member]);
    }

    public static IEnumerable<ValidationResult> Selector(string member, string label, string selector, SourceFormat format, bool required)
    {
        if (string.IsNullOrWhiteSpace(selector))
        {
            if (required)
                yield return new ValidationResult($"{label} is required.", [member]);
            yield break;
        }

        if (SourceContent.TryCompile(selector, format) is { } problem)
            yield return new ValidationResult($"{label} is not a valid {SelectorWord(format)}: {problem}", [member]);
    }

    public static IEnumerable<ValidationResult> Field(string member, string label, SelectorField field, SourceFormat format, bool required)
    {
        if (IsUnused(field, format))
        {
            if (required)
                yield return new ValidationResult($"{label} is required.", [member]);
            yield break;
        }

        if (!string.IsNullOrWhiteSpace(field.Selector) && SourceContent.TryCompile(field.Selector, format) is { } problem)
            yield return new ValidationResult($"{label} is not a valid {SelectorWord(format)}: {problem}", [member]);
        if (!string.IsNullOrEmpty(field.Regex) && PatternProblem(field.Regex) is { } regexProblem)
            yield return new ValidationResult($"{label} regex is not valid: {regexProblem}", [member]);
    }
}

/// <summary>
/// Extracts one value from an item (a search result, a chapter). For HTML, <see cref="Selector"/> picks the element
/// (relative to the item the field belongs to; empty = the item itself) and <see cref="Attribute"/> picks what to read
/// (empty = the element's text). For JSON (<see cref="SourceFormat.Json"/>), <see cref="Selector"/> is the path to the
/// value inside the item and there are no attributes. The optional <see cref="Regex"/> post-processes the result.
/// </summary>
public sealed record SelectorField
{
    /// <summary>
    /// CSS selector or XPath expression (XPath when it starts with <c>/</c>, <c>./</c>, <c>../</c> or <c>(</c>).
    /// An XPath may end in <c>/@attribute</c> or <c>/text()</c>. For JSON a path such as <c>title</c>,
    /// <c>manga.cover</c> or <c>names[0]</c>; alternatives separated by commas are tried in order
    /// (<c>english_title, title</c>) and <c>$</c> is the item itself.
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

    /// <summary>
    /// When set, <see cref="Regex"/> replaces every match with this text instead of extracting. The placeholders of the
    /// step's URL template can be used (<c>{url}</c>, the named groups of the URL patterns, ...), which is how a link is
    /// built from an id: regex <c>^(.+)$</c>, replacement <c>/series/{slug}/chapter/$1</c>.
    /// </summary>
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

    /// <summary>
    /// Everything that is wrong with the search section. <paramref name="member"/> is the name it has in its parent (for
    /// the error keys), <paramref name="format"/> the <see cref="SelectorDownloaderSettings.Format"/> of the source.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(string member, SourceFormat format) => SelectorValidation
        .Template($"{member}.{nameof(UrlTemplate)}", "Search URL", UrlTemplate, ["query", "page"], required: true)
        .Concat(SelectorValidation.Selector($"{member}.{nameof(ItemSelector)}", "Search result item selector", ItemSelector, format, required: true))
        .Concat(SelectorValidation.Field($"{member}.{nameof(Title)}", "Search result title", Title, format, required: true))
        .Concat(SelectorValidation.Field($"{member}.{nameof(Url)}", "Search result URL", Url, format, required: true))
        .Concat(SelectorValidation.Field($"{member}.{nameof(Cover)}", "Search result cover", Cover, format, required: true));
}

/// <summary>
/// How to read the page of a series. A series that is found by a search comes with its cover, one that is added by
/// pasting its URL does not: there is only the page of the series to take it from. Optional: without a cover field
/// such a series is added without the cover of the website.
/// </summary>
public sealed record SeriesSpec
{
    /// <summary>
    /// URL of the page that shows the series. <c>{url}</c> is the series URL (the default, for sites where it is the page
    /// itself); the named groups of <see cref="SelectorDownloaderSettings.SeriesUrlPattern"/> are available too, e.g.
    /// <c>/api/manga/{slug}</c> for a site that loads its pages from an API.
    /// </summary>
    public string UrlTemplate { get; init; } = "{url}";

    /// <summary>
    /// The cover image URL, in the page of the series (for JSON the path from the root of the answer). Unlike the
    /// fields of a search result it is not relative to an item. Without it, a series added by its URL gets no cover
    /// of the website.
    /// </summary>
    public SelectorField Cover { get; init; } = new();

    /// <summary>Whether the section says where the cover is, for a source with this <paramref name="format"/>.</summary>
    public bool HasCover(SourceFormat format) => !SelectorValidation.IsUnused(Cover, format);

    /// <summary>
    /// Everything that is wrong with the series section. <paramref name="member"/> is the name it has in its parent (for
    /// the error keys), <paramref name="format"/> the <see cref="SelectorDownloaderSettings.Format"/> of the source,
    /// <paramref name="seriesUrlGroups"/> the named groups of the series URL pattern that <see cref="UrlTemplate"/> may use.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(string member, SourceFormat format, IEnumerable<string> seriesUrlGroups) => SelectorValidation
        // Nothing is requested when there is no cover to read, so the template only has to be right when it is used.
        .Template($"{member}.{nameof(UrlTemplate)}", "Series page URL", UrlTemplate, ["url", .. seriesUrlGroups], required: false)
        .Concat(SelectorValidation.Field($"{member}.{nameof(Cover)}", "Series cover", Cover, format, required: false));
}

/// <summary>How to list the chapters of a series.</summary>
public sealed record ChaptersSpec
{
    /// <summary>
    /// URL of the page that lists the chapters. <c>{url}</c> is the series URL; the named groups of
    /// <see cref="SelectorDownloaderSettings.SeriesUrlPattern"/> are available too, e.g.
    /// <c>https://example.com/series/{id}/chapters</c>.
    /// </summary>
    public string UrlTemplate { get; init; } = "{url}";

    /// <summary>Selects one element per chapter.</summary>
    public string ItemSelector { get; init; } = "";

    /// <summary>The chapter's link, relative to the chapter item.</summary>
    public SelectorField Url { get; init; } = new();

    /// <summary>
    /// The chapter number, relative to the chapter item. When unused the item's text is searched (HTML only: for JSON
    /// it is required). The first number in the extracted text is used (<c>Chapter 12.5</c> becomes <c>12.5</c>);
    /// chapters without one are skipped.
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

    /// <summary>
    /// Everything that is wrong with the chapters section. <paramref name="member"/> is the name it has in its parent (for
    /// the error keys), <paramref name="format"/> the <see cref="SelectorDownloaderSettings.Format"/> of the source,
    /// <paramref name="seriesUrlGroups"/> the named groups of the series URL pattern that <see cref="UrlTemplate"/> may use.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(string member, SourceFormat format, IEnumerable<string> seriesUrlGroups) => SelectorValidation
        .Template($"{member}.{nameof(UrlTemplate)}", "Chapter list URL", UrlTemplate, ["url", .. seriesUrlGroups], required: true)
        .Concat(SelectorValidation.Selector($"{member}.{nameof(ItemSelector)}", "Chapter item selector", ItemSelector, format, required: true))
        .Concat(SelectorValidation.Field($"{member}.{nameof(Url)}", "Chapter URL", Url, format, required: true))
        // A JSON item is an object and has no text of its own to look for a number in.
        .Concat(SelectorValidation.Field($"{member}.{nameof(Number)}", "Chapter number", Number, format, required: format == SourceFormat.Json))
        .Concat(SelectorValidation.Field($"{member}.{nameof(Title)}", "Chapter title", Title, format, required: false))
        .Concat(SelectorValidation.Field($"{member}.{nameof(Date)}", "Chapter date", Date, format, required: false))
        .Concat(SelectorValidation.Field($"{member}.{nameof(NextPage)}", "Chapter list next page", NextPage, format, required: false));
}

/// <summary>How to find the pages of a chapter.</summary>
public sealed record ImagesSpec
{
    /// <summary>
    /// URL of the page that shows the images. <c>{url}</c> is the chapter URL, e.g. <c>{url}/images</c>; the named groups
    /// of <see cref="UrlPattern"/> are available too, e.g. <c>/api/chapter/{id}</c>.
    /// </summary>
    public string UrlTemplate { get; init; } = "{url}";

    /// <summary>
    /// Optional regular expression the chapter URL is matched against. Its named groups (e.g. <c>(?&lt;id&gt;\d+)$</c>)
    /// are available as <c>{id}</c> in <see cref="UrlTemplate"/>, for sites whose images are loaded from a different
    /// URL than the chapter page (typically an API).
    /// </summary>
    public string? UrlPattern { get; init; }

    /// <summary>Selects one element per page image, in reading order. For JSON the path to the image URLs, e.g. <c>pages</c> or <c>pages[*].url</c>.</summary>
    public string ImageSelector { get; init; } = "";

    /// <summary>Attribute holding the image URL. Defaults to <c>data-src,data-lazy-src,src</c>. Not used for JSON.</summary>
    public string? Attribute { get; init; }

    /// <summary>
    /// Only for sites that show one image per HTML page: selects the links (or the options of a page dropdown) that
    /// lead to each page of the chapter, in reading order. The attribute defaults to <c>href,value</c>. Every page
    /// found is then fetched and the image selector is applied to it. The chapter page itself is not fetched twice
    /// when it is one of the pages. When the selector finds nothing, the image selector is applied to the chapter
    /// page, as if this was not set.
    /// </summary>
    public SelectorField PageSelector { get; init; } = new();

    /// <summary>The names of the groups of <see cref="UrlPattern"/> (usable as <c>{name}</c> in <see cref="UrlTemplate"/>). Empty without a valid pattern.</summary>
    public IReadOnlyCollection<string> UrlGroups() => SelectorValidation.GroupNames(UrlPattern);

    /// <summary>
    /// Everything that is wrong with the pages section. <paramref name="member"/> is the name it has in its parent (for
    /// the error keys), <paramref name="format"/> the <see cref="SelectorDownloaderSettings.Format"/> of the source.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(string member, SourceFormat format)
    {
        if (SelectorValidation.PatternProblem(UrlPattern) is { } problem)
            yield return new ValidationResult($"Chapter URL pattern is not a valid regular expression: {problem}", [$"{member}.{nameof(UrlPattern)}"]);
        foreach (ValidationResult result in SelectorValidation.Template($"{member}.{nameof(UrlTemplate)}", "Chapter pages URL", UrlTemplate, ["url", .. UrlGroups()], required: true))
            yield return result;
        foreach (ValidationResult result in SelectorValidation.Selector($"{member}.{nameof(ImageSelector)}", "Page image selector", ImageSelector, format, required: true))
            yield return result;
        foreach (ValidationResult result in SelectorValidation.Field($"{member}.{nameof(PageSelector)}", "Chapter page list", PageSelector, format, required: false))
            yield return result;
        if (!SelectorValidation.IsUnused(PageSelector, format) && string.IsNullOrWhiteSpace(PageSelector.Selector))
            yield return new ValidationResult("Chapter page list needs a selector for the page links.", [$"{member}.{nameof(PageSelector)}"]);
    }
}
