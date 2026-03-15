using Common.Datatypes;
using MetadataExtensions.Extensions;

namespace MetadataExtensions;

public static class MetadataExtensionsCollection
{
    public static readonly MangaUpdates MangaUpdates = new ();

    public static async Task<List<ComicInfo>> SearchAll(SearchQuery searchQuery, CancellationToken ct)
    {
        List<ComicInfo> ret = new ();
        
        // TODO multi-thread
        if(await MangaUpdates.Search(searchQuery, ct) is { } mangaUpdatesSearchResult)
            ret.AddRange(mangaUpdatesSearchResult);

        return ret;
    }
}