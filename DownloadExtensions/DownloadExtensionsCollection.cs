using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

    public static List<MangaInfo> SearchAll(SearchQuery query, CancellationToken ct) => Search(query, Extensions, ct);
    
    public static List<MangaInfo> Search(SearchQuery searchQuery, IDownloadExtension[] extensions, CancellationToken ct)
    {
        List<Task<List<MangaInfo>?>> tasks = extensions.Select(e => e.Search(searchQuery, ct)).ToList();
        
        Task.WaitAll(tasks, ct);
        
        List<MangaInfo> ret = tasks
            .Where(t => t is { IsCompleted: true, Result: not null })
            .SelectMany(t => t.Result!).ToList();

        return ret;
    }
    
}