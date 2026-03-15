using Common.Datatypes;
using DownloadExtensions.Data;
using DownloadExtensions.Extensions;

namespace DownloadExtensions;

public static class DownloadExtensionsCollection
{
    public static readonly IDownloadExtension[] Extensions =
    [
        new MangaDex()
    ];

    public static List<MangaInfo> SearchAll(SearchQuery query, CancellationToken ct)
    {
        List<Task<MangaSearchResult?>> tasks = Extensions.Select(e => e.Search(query, ct)).ToList();
        foreach (Task<MangaSearchResult?> task in tasks)
            task.Start();
        
        Task.WaitAll(tasks, ct);
        
        List<MangaInfo> ret = tasks
            .Where(t => t is { IsCompleted: true, Result: not null })
            .SelectMany(t => t.Result!).ToList();

        return ret;
    }
    
}