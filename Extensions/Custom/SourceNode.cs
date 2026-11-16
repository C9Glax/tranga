using System.Text.Json;
using AngleSharp.Dom;

namespace Extensions.Custom;

/// <summary>
/// A response of a website, or one item in it (a search result, a chapter), independent of whether the website answers
/// with HTML or JSON. It is what <see cref="SelectorDownloadExtension"/> works on, so the engine itself does not know
/// the format: HTML is read with CSS/XPath (<see cref="SelectorEvaluator"/>), JSON with paths
/// (<see cref="JsonPathEvaluator"/>).
/// </summary>
internal interface ISourceNode
{
    /// <summary>The nodes <paramref name="selector"/> selects below this one, in order.</summary>
    List<ISourceNode> Select(string selector);

    /// <summary>The value of a field of this node. Null when the field is unused or nothing was found.</summary>
    string? Text(SelectorField field);

    /// <summary>Like <see cref="Text"/>, resolved against <paramref name="pageUrl"/>. Null for anything that is not an http(s) URL.</summary>
    string? Url(SelectorField field, Uri pageUrl);

    /// <summary>
    /// This node itself as a link. HTML reads the first of <paramref name="defaultAttributes"/> (or of the attributes of
    /// <paramref name="transform"/>) that has a value, JSON takes the value. The regex of <paramref name="transform"/> is applied.
    /// </summary>
    string? OwnUrl(SelectorField transform, string defaultAttributes, Uri pageUrl);

    /// <summary>What this node shows as text: the text of an HTML element, the value of a JSON string or number (otherwise empty).</summary>
    string OwnText { get; }
}

/// <summary>Turns the body of a response into an <see cref="ISourceNode"/>, and checks selectors, for a <see cref="SourceFormat"/>.</summary>
internal static class SourceContent
{
    /// <exception cref="JsonException">The format is JSON and the body is not</exception>
    internal static ISourceNode Parse(string body, SourceFormat format)
    {
        if (format != SourceFormat.Json)
            return new HtmlNode(SelectorEvaluator.Parse(body), isDocument: true);

        using JsonDocument document = JsonDocument.Parse(body);
        return new JsonNode(document.RootElement.Clone());
    }

    /// <summary>Checks that the selector (CSS/XPath) or path (JSON) can be used. Returns null when it is fine, otherwise the problem.</summary>
    internal static string? TryCompile(string selector, SourceFormat format) =>
        format == SourceFormat.Json ? JsonPathEvaluator.TryCompile(selector) : SelectorEvaluator.TryCompile(selector);

    private sealed class HtmlNode(INode node, bool isDocument) : ISourceNode
    {
        public List<ISourceNode> Select(string selector) =>
            SelectorEvaluator.SelectAll(node, selector, relative: !isDocument).Select(ISourceNode (n) => new HtmlNode(n, isDocument: false)).ToList();

        public string? Text(SelectorField field) => SelectorEvaluator.ExtractText(node, field);

        public string? Url(SelectorField field, Uri pageUrl) => SelectorEvaluator.ExtractUrl(node, field, pageUrl);

        public string? OwnUrl(SelectorField transform, string defaultAttributes, Uri pageUrl) => SelectorEvaluator.ExtractUrl(node,
            transform with { Selector = "", Attribute = string.IsNullOrWhiteSpace(transform.Attribute) ? defaultAttributes : transform.Attribute }, pageUrl);

        public string OwnText => SelectorEvaluator.NormalizedText(node);
    }

    private sealed class JsonNode(JsonElement element) : ISourceNode
    {
        public List<ISourceNode> Select(string selector) =>
            JsonPathEvaluator.SelectAll(element, selector).Select(ISourceNode (e) => new JsonNode(e)).ToList();

        public string? Text(SelectorField field)
        {
            // There are no attributes in JSON: a field is its path (and what the regex makes of the value).
            if (string.IsNullOrWhiteSpace(field.Selector) && string.IsNullOrEmpty(field.Regex))
                return null;

            IEnumerable<JsonElement> candidates = string.IsNullOrWhiteSpace(field.Selector) ? [element] : JsonPathEvaluator.SelectAll(element, field.Selector);
            return candidates.Select(JsonPathEvaluator.ScalarText).OfType<string>().FirstOrDefault() is { } raw
                ? SelectorEvaluator.ApplyRegex(raw, field)
                : null;
        }

        public string? Url(SelectorField field, Uri pageUrl) => Text(field) is { } value ? SelectorEvaluator.ResolveUrl(value, pageUrl) : null;

        public string? OwnUrl(SelectorField transform, string defaultAttributes, Uri pageUrl) =>
            JsonPathEvaluator.ScalarText(element) is { } raw && SelectorEvaluator.ApplyRegex(raw, transform) is { } value
                ? SelectorEvaluator.ResolveUrl(value, pageUrl)
                : null;

        public string OwnText => JsonPathEvaluator.ScalarText(element) ?? "";
    }
}
