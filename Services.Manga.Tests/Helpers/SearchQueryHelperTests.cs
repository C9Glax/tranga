using Common.Datatypes;
using Services.Manga.Database;
using Services.Manga.Helpers;

namespace Services.Manga.Tests.Helpers;

public class SearchQueryHelperTests
{
    [Fact]
    public void ToSearchQuery_UsesMetadataSeriesAsTitle()
    {
        DbManga manga = new() { MangaId = Guid.NewGuid(), Monitored = true };
        DbMetadata metadata = new()
        {
            MetadataId = Guid.NewGuid(),
            MetadataExtension = Guid.NewGuid(),
            Identifier = "id",
            Series = "One Piece"
        };
        DbMangaMetadataEntries entry = new()
        {
            MangaId = manga.MangaId,
            MetadataId = metadata.MetadataId,
            Chosen = true,
            Manga = manga,
            Metadata = metadata
        };

        SearchQuery query = entry.ToSearchQuery();

        Assert.Equal("One Piece", query.Title);
    }
}
