using Extensions.Data;
using Services.Manga.Database;
using Services.Manga.Helpers;

namespace Services.Manga.Tests.Helpers;

// NOTE: production class `ChapterInfoHelper` actually lives in the source file `MangaInfoHelper.cs`
// - see the matching note in MangaInfoHelperTests.cs.
public class ChapterInfoHelperTests
{
    [Fact]
    public void ToChapterInfo_ConvertsDbChapterDownloadLinkToChapterInfo()
    {
        Guid extensionId = Guid.NewGuid();
        DbChapterDownloadLink link = new()
        {
            ChapterId = Guid.NewGuid(),
            DownloadExtension = extensionId,
            Identifier = "chapter-1",
            Priority = 0,
            Url = "https://example.com/chapter-1"
        };

        ChapterInfo info = link.ToChapterInfo();

        Assert.Equal(extensionId, info.ExtensionIdentifier);
        Assert.Equal("https://example.com/chapter-1", info.Url);
        Assert.Equal("chapter-1", info.Identifier);
    }

    [Fact]
    public void ToChapterInfo_DefaultsUrlToEmptyStringWhenNull()
    {
        DbChapterDownloadLink link = new()
        {
            ChapterId = Guid.NewGuid(),
            DownloadExtension = Guid.NewGuid(),
            Identifier = "chapter-1",
            Priority = 0,
            Url = null
        };

        ChapterInfo info = link.ToChapterInfo();

        Assert.Equal(string.Empty, info.Url);
    }
}
