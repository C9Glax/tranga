using Common.Datatypes;
using Common.Settings;
using Extensions.Data;
using Extensions.Extensions;

namespace Extensions;

public static class DownloadExtensionsCollection
{
    public static readonly IDownloadExtension[] Extensions = BuildExtensions();

    private static IDownloadExtension[] BuildExtensions()
    {
        List<IDownloadExtension> extensions = [new MangaDex(), new AsuraScans(), new MangaPlus()];
        if (WeebCentral.IsAvailable)
            extensions.Add(new WeebCentral());
        return [.. extensions];
    }

    public static IDownloadExtension? GetExtension(Guid extensionId) => Extensions.FirstOrDefault(e => e.Identifier == extensionId);

    public static List<MangaInfo> SearchAll(SearchQuery query, CancellationToken ct) => Search(query, Extensions, ct);

    public static List<MangaInfo> Search(SearchQuery searchQuery, IDownloadExtension[] extensions, CancellationToken ct)
    {
        if (searchQuery.Language is null)
            searchQuery = searchQuery with { Language = Settings.DownloadLanguage };

        List<Task<List<MangaInfo>?>> tasks = extensions.Select(e => e.SearchDownload(searchQuery, ct)).ToList();
        
        Task.WaitAll(tasks, ct);
        
        List<MangaInfo> ret = tasks
            .Where(t => t is { IsCompleted: true, Result: not null })
            .SelectMany(t => t.Result!).ToList();

        return ret;
    }
    
}