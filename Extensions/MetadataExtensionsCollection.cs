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
        new MyAnimeList()
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