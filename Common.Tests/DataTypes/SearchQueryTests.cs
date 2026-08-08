using Common.Datatypes;

namespace Common.Tests.DataTypes;

public class SearchQueryTests
{
    [Fact]
    public void CanBeConstructedWithNoParameters()
    {
        SearchQuery query = new();

        Assert.Null(query.Title);
        Assert.Null(query.Tags);
        Assert.Null(query.ContentRating);
        Assert.Null(query.Year);
        Assert.Null(query.Author);
        Assert.Null(query.Artist);
        Assert.Null(query.Language);
        Assert.Null(query.MangaUpdatesSeriesId);
        Assert.Null(query.MangaDexSeriesId);
    }

    [Fact]
    public void CanBeConstructedWithIndividualParameters()
    {
        Assert.Equal("One Piece", new SearchQuery(Title: "One Piece").Title);
        Assert.Equal(["action", "adventure"], new SearchQuery(Tags: ["action", "adventure"]).Tags ?? []);
        Assert.Equal(ContentRating.Safe, new SearchQuery(ContentRating: ContentRating.Safe).ContentRating);
        Assert.Equal(1999, new SearchQuery(Year: 1999).Year);
        Assert.Equal("Eiichiro Oda", new SearchQuery(Author: "Eiichiro Oda").Author);
        Assert.Equal("Eiichiro Oda", new SearchQuery(Artist: "Eiichiro Oda").Artist);
        Assert.Equal("en", new SearchQuery(Language: "en").Language);
        Assert.Equal(1L, new SearchQuery(MangaUpdatesSeriesId: 1L).MangaUpdatesSeriesId);
        Guid id = Guid.NewGuid();
        Assert.Equal(id, new SearchQuery(MangaDexSeriesId: id).MangaDexSeriesId);
    }

    [Fact]
    public void CanBeConstructedWithMultipleParameters()
    {
        Guid mangaDexId = Guid.NewGuid();
        SearchQuery query = new(
            Title: "One Piece",
            Tags: ["action", "adventure"],
            ContentRating: ContentRating.Safe,
            Year: 1999,
            Author: "Eiichiro Oda",
            Artist: "Eiichiro Oda",
            Language: "en",
            MangaUpdatesSeriesId: 42L,
            MangaDexSeriesId: mangaDexId);

        Assert.Equal("One Piece", query.Title);
        Assert.Equal(["action", "adventure"], query.Tags ?? []);
        Assert.Equal(ContentRating.Safe, query.ContentRating);
        Assert.Equal(1999, query.Year);
        Assert.Equal("Eiichiro Oda", query.Author);
        Assert.Equal("Eiichiro Oda", query.Artist);
        Assert.Equal("en", query.Language);
        Assert.Equal(42L, query.MangaUpdatesSeriesId);
        Assert.Equal(mangaDexId, query.MangaDexSeriesId);
    }

    [Fact]
    public void IsARecordWithValueEquality()
    {
        SearchQuery a = new(Title: "One Piece", Year: 1999);
        SearchQuery b = new(Title: "One Piece", Year: 1999);

        Assert.Equal(a, b);
        Assert.True(a == b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void InstancesWithDifferentValuesAreNotEqual()
    {
        SearchQuery a = new(Title: "One Piece");
        SearchQuery b = new(Title: "Naruto");

        Assert.NotEqual(a, b);
        Assert.False(a == b);
    }

    [Fact]
    public void WithExpressionProducesModifiedCopyWithoutMutatingOriginal()
    {
        SearchQuery original = new(Title: "One Piece", Year: 1999);
        SearchQuery modified = original with { Year = 2000 };

        Assert.Equal(1999, original.Year);
        Assert.Equal(2000, modified.Year);
        Assert.Equal("One Piece", modified.Title);
        Assert.NotEqual(original, modified);
    }
}