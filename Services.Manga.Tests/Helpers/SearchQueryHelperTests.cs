using Common.Datatypes;
using Extensions.Extensions;
using Services.Manga.Database;
using Services.Manga.Helpers;

namespace Services.Manga.Tests.Helpers;

public class SearchQueryHelperTests
{
    private static DbMangaMetadataEntries BuildEntry(Guid metadataExtension, string identifier, string series = "One Piece")
    {
        DbManga manga = new() { MangaId = Guid.NewGuid(), Monitored = true };
        DbMetadata metadata = new()
        {
            MetadataId = Guid.NewGuid(),
            MetadataExtension = metadataExtension,
            Identifier = identifier,
            Series = series
        };
        return new()
        {
            MangaId = manga.MangaId,
            MetadataId = metadata.MetadataId,
            Chosen = true,
            Manga = manga,
            Metadata = metadata
        };
    }

    [Fact]
    public void ToSearchQuery_UsesMetadataSeriesAsTitle()
    {
        DbMangaMetadataEntries entry = BuildEntry(Guid.NewGuid(), "id");

        SearchQuery query = entry.ToSearchQuery();

        Assert.Equal("One Piece", query.Title);
        Assert.Null(query.MangaDexSeriesId);
        Assert.Null(query.MangaUpdatesSeriesId);
    }

    [Fact]
    public void ToSearchQuery_MangaDexMetadata_SetsMangaDexSeriesId()
    {
        Guid mangaDexId = Guid.NewGuid();
        DbMangaMetadataEntries entry = BuildEntry(new MangaDex().Identifier, mangaDexId.ToString());

        SearchQuery query = entry.ToSearchQuery();

        Assert.Equal(mangaDexId, query.MangaDexSeriesId);
        Assert.Null(query.MangaUpdatesSeriesId);
    }

    [Fact]
    public void ToSearchQuery_AniListMetadata_SetsAniListSeriesId()
    {
        DbMangaMetadataEntries entry = BuildEntry(new AniList().Identifier, "118586");

        SearchQuery query = entry.ToSearchQuery();

        Assert.Equal(118586, query.AniListSeriesId);
        Assert.Null(query.MyAnimeListSeriesId);
    }

    [Fact]
    public void ToSearchQuery_MyAnimeListMetadata_SetsMyAnimeListSeriesId()
    {
        DbMangaMetadataEntries entry = BuildEntry(new MyAnimeList().Identifier, "126287");

        SearchQuery query = entry.ToSearchQuery();

        Assert.Equal(126287, query.MyAnimeListSeriesId);
        Assert.Null(query.AniListSeriesId);
    }

    [Fact]
    public void ToSearchQuery_MangaUpdatesMetadata_SetsMangaUpdatesSeriesId()
    {
        DbMangaMetadataEntries entry = BuildEntry(new MangaUpdates().Identifier, "12345");

        SearchQuery query = entry.ToSearchQuery();

        Assert.Equal(12345L, query.MangaUpdatesSeriesId);
        Assert.Null(query.MangaDexSeriesId);
    }
}
