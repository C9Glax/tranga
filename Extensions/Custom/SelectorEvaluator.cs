using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using AngleSharp.XPath;

namespace Extensions.Custom;

/// <summary>
/// Evaluates <see cref="SelectorField"/>s and plain selectors against an HTML document. Pure (no I/O), so it can be
/// unit tested on HTML fixtures. CSS selectors are handled by AngleSharp (so <c>:not()</c>, <c>:has()</c>,
/// <c>:contains()</c>, ... work), XPath by AngleSharp.XPath.
/// </summary>
internal static class SelectorEvaluator
{
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled, SelectorDownloaderDefinition.RegexTimeout);

    // A srcset candidate list such as "a.webp 1x, b.webp 2x" or "a.webp 400w"
    private static readonly Regex SrcSet = new(@"\s\d+(\.\d+)?[wx](\s*,|\s*$)", RegexOptions.Compiled, SelectorDownloaderDefinition.RegexTimeout);

    private static readonly ConcurrentDictionary<string, Regex> RegexCache = new();

    /// <summary>Parses HTML the way a browser would.</summary>
    internal static IDocument Parse(string html) => new HtmlParser().ParseDocument(html);

    /// <summary>True when the selector is an XPath expression rather than a CSS selector.</summary>
    internal static bool IsXPath(string selector)
    {
        string s = selector.TrimStart();
        return s.StartsWith('/') || s.StartsWith("./") || s.StartsWith("../") || s.StartsWith('(');
    }

    /// <summary>Checks that the selector can be compiled. Returns null when it is fine, otherwise the problem.</summary>
    internal static string? TryCompile(string selector)
    {
        try
        {
            IDocument empty = Parse("");
            _ = SelectAll(empty, selector, relative: false);
            return null;
        }
        catch (Exception e)
        {
            return e.Message;
        }
    }

    /// <summary>
    /// Selects all nodes matching <paramref name="selector"/> below <paramref name="context"/>. XPath expressions that
    /// end in an attribute (<c>//a/@href</c>) select the attribute nodes.
    /// </summary>
    /// <param name="context">Element (or document) to search in</param>
    /// <param name="selector">CSS selector or XPath</param>
    /// <param name="relative">True when <paramref name="context"/> is an item and not the document: absolute <c>//x</c> XPaths (what browsers "copy") then only search inside the item</param>
    internal static List<INode> SelectAll(INode context, string selector, bool relative)
    {
        selector = selector.Trim();
        if (IsXPath(selector))
        {
            string path = relative && selector.StartsWith("//") ? "." + selector : selector;
            IElement? element = context as IElement ?? (context as IDocument)?.DocumentElement;
            return element?.SelectNodes(path)?.ToList() ?? [];
        }

        return context is IParentNode parent ? parent.QuerySelectorAll(selector).Cast<INode>().ToList() : [];
    }

    /// <summary>Extracts the text value of a field from an item. Null when the field is unused or nothing was found.</summary>
    internal static string? ExtractText(INode item, SelectorField field)
    {
        if (field.IsUnused)
            return null;
        return ExtractRaw(item, field) is { } raw ? ApplyRegex(raw, field) : null;
    }

    /// <summary>Extracts a URL from an item and resolves it against <paramref name="pageUrl"/>.</summary>
    internal static string? ExtractUrl(INode item, SelectorField field, Uri pageUrl)
    {
        if (field.IsUnused)
            return null;
        if (ExtractRaw(item, field) is not { } raw || ApplyRegex(raw, field) is not { } value)
            return null;
        return ResolveUrl(value, pageUrl);
    }

    private static string? ExtractRaw(INode item, SelectorField field)
    {
        List<INode> nodes = string.IsNullOrWhiteSpace(field.Selector) ? [item] : SelectAll(item, field.Selector, relative: true);
        string[] attributes = (field.Attribute ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (INode node in nodes)
        {
            // An XPath that ends in "/@attr" selects the attribute itself.
            if (node is IAttr attr)
            {
                if (attr.Value.Trim() is { Length: > 0 } attrValue)
                    return attrValue;
                continue;
            }

            if (attributes.Length == 0)
            {
                if (NormalizedText(node) is { Length: > 0 } text)
                    return text;
                continue;
            }

            if (node is not IElement element)
                continue;
            foreach (string attribute in attributes)
                if (element.GetAttribute(attribute)?.Trim() is { Length: > 0 } value)
                    return value;
        }

        return null;
    }

    /// <summary>The node's text with whitespace collapsed (what a browser shows).</summary>
    internal static string NormalizedText(INode node) => Whitespace.Replace(node.TextContent, " ").Trim();

    internal static string? ApplyRegex(string value, SelectorField field)
    {
        if (string.IsNullOrEmpty(field.Regex))
            return value;

        try
        {
            Regex regex = RegexCache.GetOrAdd(field.Regex,
                pattern => new Regex(pattern, RegexOptions.None, SelectorDownloaderDefinition.RegexTimeout));
            if (!string.IsNullOrEmpty(field.Replacement))
                return regex.Replace(value, field.Replacement);
            if (regex.Match(value) is not { Success: true } match)
                return null;
            return (match.Groups.Count > 1 ? match.Groups[1].Value : match.Value).Trim();
        }
        catch (Exception e) when (e is RegexMatchTimeoutException or ArgumentException)
        {
            return null;
        }
    }

    /// <summary>
    /// Resolves a (possibly relative or protocol-relative) URL. For a <c>srcset</c> style candidate list the first
    /// candidate is used. Returns null for anything that is not http(s).
    /// </summary>
    internal static string? ResolveUrl(string value, Uri pageUrl)
    {
        value = value.Trim();
        if (value.Length == 0)
            return null;
        if (SrcSet.IsMatch(value))
            value = value.Split(',')[0].Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

        return Uri.TryCreate(pageUrl, value, out Uri? resolved) && resolved.Scheme is "http" or "https"
            ? resolved.GetLeftPart(UriPartial.Query)
            : null;
    }
}
