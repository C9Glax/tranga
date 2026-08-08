using Common.Datatypes;

namespace Common.Tests.DataTypes;

public class ContentRatingTests
{
    [Theory]
    [InlineData("Safe", ContentRating.Safe)]
    [InlineData("Suggestive", ContentRating.Suggestive)]
    [InlineData("Erotica", ContentRating.Erotica)]
    [InlineData("Pornographic", ContentRating.Pornographic)]
    public void TryParseContentRatingParsesValidRatingStrings(string rating, ContentRating expected)
    {
        Assert.Equal(expected, rating.TryParseContentRating());
    }

    [Theory]
    [InlineData("safe", ContentRating.Safe)]
    [InlineData("SUGGESTIVE", ContentRating.Suggestive)]
    [InlineData("eRoTiCa", ContentRating.Erotica)]
    public void TryParseContentRatingIsCaseInsensitive(string rating, ContentRating expected)
    {
        Assert.Equal(expected, rating.TryParseContentRating());
    }

    [Theory]
    [InlineData(ContentRating.Safe, false)]
    [InlineData(ContentRating.Suggestive, false)]
    [InlineData(ContentRating.Erotica, true)]
    [InlineData(ContentRating.Pornographic, true)]
    public void IsNsfwReturnsCorrectValues(ContentRating rating, bool expected)
    {
        Assert.Equal(expected, rating.IsNsfw());
    }
}