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
        if (searchQuery.Language is null)
            searchQuery = searchQuery with { Language = Settings.DownloadLanguage };

        List<Task<List<SearchResult>?>> tasks = extensions.Select(e => e.SearchMetadata(searchQuery, ct)).ToList();
        
        Task.WaitAll(tasks, ct);
        
        List<SearchResult> ret = tasks
            .Where(t => t is { IsCompleted: true, Result: not null })
            .SelectMany(t => t.Result!).ToList();

        return ret;
    }
}