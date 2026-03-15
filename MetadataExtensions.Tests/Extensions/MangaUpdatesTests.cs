using Common.Datatypes;
using MetadataExtensions.Extensions;

namespace MetadataExtensions.Tests.Extensions;

public sealed class MangaUpdatesTests : IMetadataExtensionTests<MangaUpdates>
{
    [Fact]
    public async Task SearchReturnsManga()
    {
        SearchQuery searchQuery = new()
        {
            Title = "Sousou no Frieren"
        };
        List<ComicInfo>? result = await _metadataExtension.Search(searchQuery, ct);
        Assert.NotNull(result);
        ComicInfo? manga = result.FirstOrDefault(r => r.Web == "https://www.mangaupdates.com/series/ugf5dzu/sousou-no-frieren");
        Assert.NotNull(manga);
    }
}