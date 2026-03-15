using Common.Datatypes;
using MetadataExtensions.Extensions;

namespace MetadataExtensions;

public static class MetadataExtensionsCollection
{
    private static readonly IMetadataExtension[] Extensions =
    [
        new MangaUpdates()
    ];

    public static List<ComicInfo> SearchAll(SearchQuery searchQuery, CancellationToken ct)
    {
        List<Task<List<ComicInfo>?>> tasks = Extensions.Select(e => e.Search(searchQuery, ct)).ToList();
        foreach (Task<List<ComicInfo>?> task in tasks)
            task.Start();
        
        Task.WaitAll(tasks, ct);
        
        List<ComicInfo> ret = tasks
            .Where(t => t is { IsCompleted: true, Result: not null })
            .SelectMany(t => t.Result!).ToList();

        return ret;
    }
}