namespace Extensions.Custom;

/// <summary>Which step of a <see cref="SelectorDownloaderDefinition"/> is tried against the real website.</summary>
public enum SelectorDownloaderStep
{
    /// <summary>Searching for a series</summary>
    Search,

    /// <summary>Listing the chapters of a series</summary>
    Chapters,

    /// <summary>Finding the pages of a chapter</summary>
    Images
}

/// <summary>
/// What one step of a source definition produced against the real website, so users can fix their selectors before
/// saving the source.
/// </summary>
public sealed record StepTestResult
{
    /// <summary>The URL that was requested, once the template has been filled in.</summary>
    public string? RequestUrl { get; init; }

    /// <summary>HTTP status of the response, when there was one.</summary>
    public int? StatusCode { get; init; }

    /// <summary>How many items (search results, chapters, images) were extracted in total.</summary>
    public int ItemCount { get; init; }

    /// <summary>The first extracted items, one dictionary of field name to value each.</summary>
    public List<Dictionary<string, string?>> Rows { get; init; } = [];

    /// <summary>Things that probably are not what the user wants (selector matched nothing, fields missing, ...).</summary>
    public List<string> Warnings { get; init; } = [];

    /// <summary>Why the step failed, if it did.</summary>
    public string? Error { get; init; }
}
