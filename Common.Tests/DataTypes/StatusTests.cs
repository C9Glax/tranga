using Common.Datatypes;

namespace Common.Tests.DataTypes;

public class StatusTests
{
    [Theory]
    [InlineData("ongoing", ReleaseStatus.Ongoing)]
    [InlineData("releasing", ReleaseStatus.Ongoing)]
    [InlineData("complete", ReleaseStatus.Complete)]
    [InlineData("completed", ReleaseStatus.Complete)]
    [InlineData("hiatus", ReleaseStatus.Hiatus)]
    // AniList spellings
    [InlineData("finished", ReleaseStatus.Complete)]
    [InlineData("cancelled", ReleaseStatus.Cancelled)]
    [InlineData("canceled", ReleaseStatus.Cancelled)]
    // MyAnimeList spellings
    [InlineData("currently_publishing", ReleaseStatus.Ongoing)]
    [InlineData("on_hiatus", ReleaseStatus.Hiatus)]
    [InlineData("discontinued", ReleaseStatus.Cancelled)]
    public void ParseStatusParsesValidStatusStrings(string status, ReleaseStatus expected)
    {
        Assert.Equal(expected, status.ParseStatus());
    }

    [Theory]
    [InlineData("ONGOING", ReleaseStatus.Ongoing)]
    [InlineData("Complete", ReleaseStatus.Complete)]
    [InlineData("HiAtUs", ReleaseStatus.Hiatus)]
    public void ParseStatusIsCaseInsensitive(string status, ReleaseStatus expected)
    {
        Assert.Equal(expected, status.ParseStatus());
    }

    [Fact]
    public void ParseStatusReturnsNullForNullInput()
    {
        string? status = null;
        Assert.Null(status.ParseStatus());
    }

    [Theory]
    [InlineData("unknown-status")]
    [InlineData("")]
    [InlineData("   ")]
    // Not-yet-published series have no ReleaseStatus equivalent
    [InlineData("not_yet_published")]
    [InlineData("not_yet_released")]
    public void ParseStatusReturnsNullForUnknownOrEmptyInput(string status)
    {
        Assert.Null(status.ParseStatus());
    }
}