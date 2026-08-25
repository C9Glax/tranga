using Common.Datatypes;
using Services.Manga.Database;
using Services.Tasks.Helpers;

namespace Services.Tasks.Tests.Helpers;

public class ComicInfoHelperTests
{
    [Fact]
    public void MapsSeriesTitleAndNumber()
    {
        DbChapter chapter = new()
        {
            MangaId = Guid.Empty,
            Number = "5",
            Title = "Title",
        };

        ConcreteComicInfo comicInfo = chapter.CreateComicInfo("Series Name");

        Assert.Equal("Series Name", comicInfo.Series);
        Assert.Equal("Title", comicInfo.Title);
        Assert.Equal("5", comicInfo.Number);
    }

    [Fact]
    public void VolumePresent_IsParsedToInt()
    {
        DbChapter chapter = new()
        {
            MangaId = Guid.Empty,
            Volume = "1",
            Number = "4",
        };

        ConcreteComicInfo comicInfo = chapter.CreateComicInfo("Series Name");

        Assert.Equal(1, comicInfo.Volume);
    }

    [Fact]
    public void VolumeMissing_DefaultsToUnspecified()
    {
        DbChapter chapter = new()
        {
            MangaId = Guid.Empty,
            Volume = null,
            Number = "5",
        };

        ConcreteComicInfo comicInfo = chapter.CreateComicInfo("Series Name");

        Assert.Equal(-1, comicInfo.Volume);
    }

    [Fact]
    public void TitleMissing_MapsToEmptyString()
    {
        DbChapter chapter = new()
        {
            MangaId = Guid.Empty,
            Number = "5",
            Title = null,
        };

        ConcreteComicInfo comicInfo = chapter.CreateComicInfo("Series Name");

        Assert.Equal(string.Empty, comicInfo.Title);
    }
}
