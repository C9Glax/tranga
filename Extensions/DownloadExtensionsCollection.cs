using Common.Datatypes;
using Extensions.Data;
using Extensions.Extensions;

namespace Extensions;

public static class DownloadExtensionsCollection
{
    public static readonly IDownloadExtension[] Extensions =
    [
        new MangaDex()
    ];

    public static IDownloadExtension? GetExtension(Guid extensionId) => Extensions.FirstOrDefault(e => e.Identifier == extensionId);

    public static List<MangaInfo> SearchAll(SearchQuery query, CancellationToken ct) => Search(query, Extensions, ct);
    
    public static List<MangaInfo> Search(SearchQuery searchQuery, IDownloadExtension[] extensions, CancellationToken ct)
    {
        List<Task<List<MangaInfo>?>> tasks = extensions.Select(e => e.SearchDownload(searchQuery, ct)).ToList();
        
        Task.WaitAll(tasks, ct);
        
        List<MangaInfo> ret = tasks
            .Where(t => t is { IsCompleted: true, Result: not null })
            .SelectMany(t => t.Result!).ToList();

        return ret;
    }
    
}