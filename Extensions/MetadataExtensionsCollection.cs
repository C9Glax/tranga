using Common.Datatypes;
using Common.Settings;
using Extensions.Data;
using Extensions.Extensions;

namespace Extensions;

public static class MetadataExtensionsCollection
{
    public static readonly IMetadataExtension[] Extensions =
    [
        new MangaUpdates(),
        new MangaDex(),
        new AniList(),
        // MyAnimeList's API rejects every request without a client-id, so without one the extension could only ever
        // return nothing. Leave it out of the collection entirely rather than offering a provider that cannot answer:
        // it then never shows up in the frontend's provider list either (see Services.Manga MetadataExtensionsList).
        .. EnvVars.MAL_CLIENT_ID is not null ? new IMetadataExtension[] { new MyAnimeList() } : []
    ];

    public static List<SearchResult> SearchAll(SearchQuery searchQuery, CancellationToken ct) =>
        Search(searchQuery, Extensions, ct);

    public static List<SearchResult> Search(SearchQuery searchQuery, IMetadataExtension[] extensions, CancellationToken ct)
    {
        return Search(extensions.Select(e => (e, searchQuery)).ToArray(), ct);
    }

    /// <summary>
    /// Runs each extension with its own query (e.g. the per-extension queries from <see cref="ParseUrl"/>). An
    /// extension that fails or throws contributes no results instead of failing the whole search.
    /// </summary>
    public static List<SearchResult> Search((IMetadataExtension Extension, SearchQuery Query)[] searches, CancellationToken ct)
    {
        List<Task<List<SearchResult>?>> tasks = searches.Select(s => SearchSafe(s.Extension,
            s.Query.Language is null ? s.Query with { Language = Settings.DownloadLanguage } : s.Query, ct)).ToList();

        Task.WaitAll(tasks, ct);

        return tasks.Where(t => t.Result is not null).SelectMany(t => t.Result!).ToList();
    }

    private static async Task<List<SearchResult>?> SearchSafe(IMetadataExtension extension, SearchQuery searchQuery, CancellationToken ct)
    {
        try
        {
            return await extension.SearchMetadata(searchQuery, ct);
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            // e.g. an unknown id from a pasted URL (404) or the provider being down: this extension just has no results.
            return null;
        }
    }

    /// <summary>
    /// Resolves a manga page URL into a direct lookup on every extension that recognises it.
    /// </summary>
    /// <param name="url">A manga page URL, e.g. <c>https://anilist.co/manga/30002</c>.</param>
    /// <param name="extensions">Extensions to try; defaults to <see cref="Extensions"/>.</param>
    /// <returns>The matching extensions with their parsed queries; empty if no extension recognises the URL.</returns>
    public static (IMetadataExtension Extension, SearchQuery Query)[] ParseUrl(string url, IMetadataExtension[]? extensions = null) =>
        (extensions ?? Extensions)
            .Select(e => (Extension: e, Query: e.ParseSearchQueryFromUrl(url)))
            .Where(p => p.Query is not null)
            .Select(p => (p.Extension, p.Query!))
            .ToArray();
}