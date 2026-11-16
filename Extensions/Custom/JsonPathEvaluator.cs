using System.Text.Json;

namespace Extensions.Custom;

/// <summary>
/// Evaluates the paths that take the place of CSS/XPath selectors for sources that answer with JSON
/// (<see cref="SourceFormat.Json"/>). Pure (no I/O). Deliberately a small subset of JSONPath, enough to walk an API
/// response:
/// <list type="bullet">
/// <item><c>data</c>, <c>manga.title</c>: properties, separated by dots. A leading <c>$</c> or <c>$.</c> is allowed, <c>$</c> alone is the value itself.</item>
/// <item><c>pages[0]</c>, <c>pages[-1]</c>: one element of an array (negative counts from the end). <c>pages[*]</c>: all of them.</item>
/// <item><c>['odd.name']</c>: a property whose name contains dots, brackets or commas.</item>
/// <item><c>english_title, title</c>: alternatives. All matches are collected in this order.</item>
/// </list>
/// A property asked of an array is asked of each of its elements, and an array at the end of a path stands for its
/// elements, so <c>data</c>, <c>data[*]</c> and <c>$.data[*]</c> select the same items.
/// </summary>
internal static class JsonPathEvaluator
{
    private abstract record Step;

    private sealed record Property(string Name) : Step;

    private sealed record Index(int Position) : Step;

    private sealed record All : Step;

    /// <summary>Checks that every alternative of the path can be parsed. Returns null when it is fine, otherwise the problem.</summary>
    internal static string? TryCompile(string path)
    {
        try
        {
            foreach (string alternative in SplitAlternatives(path))
                _ = Parse(alternative);
            return null;
        }
        catch (FormatException e)
        {
            return e.Message;
        }
    }

    /// <summary>
    /// Everything the path selects below <paramref name="context"/>, in document order; arrays at the end are replaced
    /// by their elements and <c>null</c>s are left out.
    /// </summary>
    /// <exception cref="FormatException">The path is not valid (see <see cref="TryCompile"/>)</exception>
    internal static List<JsonElement> SelectAll(JsonElement context, string path)
    {
        List<JsonElement> selected = [];
        foreach (string alternative in SplitAlternatives(path))
        {
            List<JsonElement> current = [context];
            foreach (Step step in Parse(alternative))
                current = current.SelectMany(element => Apply(step, element)).ToList();
            selected.AddRange(current.SelectMany(IEnumerable<JsonElement> (element) => element.ValueKind == JsonValueKind.Array ? element.EnumerateArray().ToList() : [element]));
        }

        return selected.Where(element => element.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)).ToList();
    }

    /// <summary>The text of a value: a string as it is, a number or boolean as written. Null for null, objects, arrays and empty strings.</summary>
    internal static string? ScalarText(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString()?.Trim() is { Length: > 0 } text ? text : null,
        JsonValueKind.Number => element.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => null
    };

    private static IEnumerable<JsonElement> Apply(Step step, JsonElement element)
    {
        switch (step)
        {
            case Property property when element.ValueKind == JsonValueKind.Object:
                if (element.TryGetProperty(property.Name, out JsonElement value))
                    yield return value;
                break;
            // A property of a list is the property of each of its elements.
            case Property property when element.ValueKind == JsonValueKind.Array:
                foreach (JsonElement child in element.EnumerateArray())
                    if (child.ValueKind == JsonValueKind.Object && child.TryGetProperty(property.Name, out JsonElement childValue))
                        yield return childValue;
                break;
            case Index index when element.ValueKind == JsonValueKind.Array:
                int length = element.GetArrayLength();
                int position = index.Position < 0 ? length + index.Position : index.Position;
                if (position >= 0 && position < length)
                    yield return element[position];
                break;
            case All when element.ValueKind == JsonValueKind.Array:
                foreach (JsonElement child in element.EnumerateArray())
                    yield return child;
                break;
            case All when element.ValueKind == JsonValueKind.Object:
                foreach (JsonProperty child in element.EnumerateObject())
                    yield return child.Value;
                break;
        }
    }

    /// <summary>Splits <c>a, b[0], ['c,d']</c> at the commas that are not inside brackets.</summary>
    private static List<string> SplitAlternatives(string path)
    {
        List<string> alternatives = [];
        int depth = 0, start = 0;
        char quote = '\0';
        for (int i = 0; i < path.Length; i++)
        {
            char c = path[i];
            if (quote != '\0')
                quote = c == quote ? '\0' : quote;
            else if (c is '\'' or '"')
                quote = c;
            else if (c == '[')
                depth++;
            else if (c == ']')
                depth--;
            else if (c == ',' && depth == 0)
            {
                alternatives.Add(path[start..i]);
                start = i + 1;
            }
        }

        alternatives.Add(path[start..]);
        if (alternatives.Count > 1 && alternatives.Any(string.IsNullOrWhiteSpace))
            throw new FormatException("There is nothing between two commas.");
        return alternatives;
    }

    private static List<Step> Parse(string path)
    {
        string text = path.Trim();
        // Most likely a CSS selector or XPath that was left over: a real name like this has to be written in brackets.
        if (OutsideBrackets(text).Any(c => char.IsWhiteSpace(c) || c is '>' or '#' or ':' or '=' or '/' or '@'))
            throw new FormatException($"'{text}' is not a JSON path (it looks like a CSS selector or XPath). A path is written like data.items[0].title; a property name with other characters goes in brackets: ['my name'].");

        List<Step> steps = [];
        int i = 0;
        if (text.StartsWith('$'))
            i = 1;

        bool expectName = i == 0; // at the very start a name may follow without a dot
        while (i < text.Length)
        {
            char c = text[i];
            if (c == '.')
            {
                if (i + 1 < text.Length && text[i + 1] == '.')
                    throw new FormatException("'..' (searching at any depth) is not supported. Write the path out.");
                if (i + 1 >= text.Length)
                    throw new FormatException("The path ends with a dot.");
                expectName = text[i + 1] != '[';
                i++;
            }
            else if (c == '[')
            {
                int end = ClosingBracket(text, i);
                steps.Add(ParseBracket(text[(i + 1)..end].Trim()));
                i = end + 1;
                expectName = false;
            }
            else if (expectName)
            {
                int end = text.IndexOfAny(['.', '['], i);
                if (end < 0)
                    end = text.Length;
                string name = text[i..end].Trim();
                if (name.Length == 0)
                    throw new FormatException("A name is missing in the path.");
                if (name.Contains(']') || name.Contains('?') || name.Contains('('))
                    throw new FormatException($"'{name}' is not a property name. Filters and functions are not supported.");
                steps.Add(name == "*" ? new All() : new Property(name));
                i = end;
                expectName = false;
            }
            else
            {
                throw new FormatException($"Unexpected '{c}' at position {i + 1}. Separate names with dots.");
            }
        }

        return steps;
    }

    /// <summary>The path without what is written in brackets (where any character may be part of a name).</summary>
    private static string OutsideBrackets(string text)
    {
        System.Text.StringBuilder outside = new();
        int depth = 0;
        foreach (char c in text)
        {
            if (c == '[')
                depth++;
            else if (c == ']')
                depth = Math.Max(0, depth - 1);
            else if (depth == 0)
                outside.Append(c);
        }

        return outside.ToString();
    }

    private static int ClosingBracket(string text, int open)
    {
        char quote = '\0';
        for (int i = open + 1; i < text.Length; i++)
        {
            char c = text[i];
            if (quote != '\0')
                quote = c == quote ? '\0' : quote;
            else if (c is '\'' or '"')
                quote = c;
            else if (c == ']')
                return i;
        }

        throw new FormatException("A '[' is not closed.");
    }

    private static Step ParseBracket(string content)
    {
        if (content == "*")
            return new All();
        if (int.TryParse(content, out int index))
            return new Index(index);
        if (content.Length >= 2 && content[0] is '\'' or '"' && content[^1] == content[0])
            return new Property(content[1..^1]);
        throw new FormatException($"'[{content}]' is not supported. Use [0], [-1], [*] or ['name']; filters are not supported.");
    }
}
