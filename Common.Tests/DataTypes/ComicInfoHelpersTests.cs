using Common.Datatypes;

namespace Common.Tests.DataTypes;

public class ComicInfoHelpersTests
{
    private sealed record ConcreteComicInfo : ComicInfo;

    [Fact]
    public void ToSearchQueryReturnsValidSearchQuery()
    {
        ConcreteComicInfo info = new() { Title = "One Piece" };

        SearchQuery query = info.ToSearchQuery();

        Assert.IsType<SearchQuery>(query);
    }

    [Fact]
    public void ToSearchQueryMapsTitle()
    {
        ConcreteComicInfo info = new() { Title = "One Piece" };

        SearchQuery query = info.ToSearchQuery();

        Assert.Equal("One Piece", query.Title);
    }

    [Fact]
    public void ToSearchQueryOnlyMapsTitleCurrently()
    {
        ConcreteComicInfo info = new()
        {
            Title = "One Piece",
            Writer = "Eiichiro Oda",
            Genre = "Action",
            Year = 1997,
        };

        SearchQuery query = info.ToSearchQuery();

        Assert.Null(query.Tags);
        Assert.Null(query.ContentRating);
        Assert.Null(query.Year);
        Assert.Null(query.Author);
        Assert.Null(query.Artist);
        Assert.Null(query.Language);
    }

    [Fact]
    public void ToSearchQueryHandlesEmptyTitle()
    {
        ConcreteComicInfo info = new();

        SearchQuery query = info.ToSearchQuery();

        Assert.Equal("", query.Title);
    }
}