using Common.Datatypes;
using MetadataExtensions.Extensions;

namespace MetadataExtensions;

public static class MetadataExtensionsCollection
{
    public static readonly IMetadataExtension[] Extensions =
    [
        new MangaUpdates(),
        new MangaDex()
    ];

    public static List<SearchResult> SearchAll(SearchQuery searchQuery, CancellationToken ct) =>
        Search(searchQuery, Extensions, ct);

    public static List<SearchResult> Search(SearchQuery searchQuery, IMetadataExtension[] extensions, CancellationToken ct)
    {
        List<Task<List<SearchResult>?>> tasks = extensions.Select(e => e.Search(searchQuery, ct)).ToList();
        
        Task.WaitAll(tasks, ct);
        
        List<SearchResult> ret = tasks
            .Where(t => t is { IsCompleted: true, Result: not null })
            .SelectMany(t => t.Result!).ToList();

        return ret;
    }
}