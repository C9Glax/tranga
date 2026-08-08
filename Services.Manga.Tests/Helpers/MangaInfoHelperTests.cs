using Extensions.Data;
using Services.Manga.Database;
using Services.Manga.Helpers;

namespace Services.Manga.Tests.Helpers;

// NOTE: production class `MangaInfoHelper` actually lives in the source file `ChapterInfoHelper.cs`
// (the two helper files in Services.Manga/Helpers swap the class <-> file name pairing). This test
// file is named after the class under test rather than mirroring that swapped file name.
public class MangaInfoHelperTests
{
    [Fact]
    public void ToChapter_ConvertsChapterInfoToDbChapterAndLinksManga()
    {
        DbManga manga = new() { MangaId = Guid.NewGuid(), Monitored = true };
        ChapterInfo info = new(Guid.NewGuid(), "12", "https://example.com/12", "chapter-12", Volume: "2", Title: "Title");

        DbChapter chapter = info.ToChapter(manga);

        Assert.NotEqual(Guid.Empty, chapter.ChapterId);
        Assert.Equal(manga.MangaId, chapter.MangaId);
        Assert.Same(manga, chapter.Manga);
        Assert.Equal("2", chapter.Volume);
        Assert.Equal("12", chapter.Number);
        Assert.Equal("Title", chapter.Title);
        Assert.NotNull(chapter.DownloadLinks);
        Assert.Empty(chapter.DownloadLinks!);
    }

    [Fact]
    public void ToChapter_GeneratesUniqueChapterIdEachCall()
    {
        DbManga manga = new() { MangaId = Guid.NewGuid(), Monitored = true };
        ChapterInfo info = new(Guid.NewGuid(), "1", "https://example.com/1", "chapter-1");

        DbChapter a = info.ToChapter(manga);
        DbChapter b = info.ToChapter(manga);

        Assert.NotEqual(a.ChapterId, b.ChapterId);
    }

    [Fact]
    public void ToChapterDownloadLink_CreatesLinkWithDefaultPriority()
    {
        DbManga manga = new() { MangaId = Guid.NewGuid(), Monitored = true };
        Guid extensionId = Guid.NewGuid();
        ChapterInfo info = new(extensionId, "1", "https://example.com/1", "chapter-1");
        DbChapter chapter = info.ToChapter(manga);

        DbChapterDownloadLink link = info.ToChapterDownloadLink(chapter);

        Assert.Equal(chapter.ChapterId, link.ChapterId);
        Assert.Same(chapter, link.Chapter);
        Assert.Equal(extensionId, link.DownloadExtension);
        Assert.Equal("chapter-1", link.Identifier);
        Assert.Equal("https://example.com/1", link.Url);
        Assert.Equal(0, link.Priority);
    }

    [Fact]
    public void CreateAndAddChapterDownloadLink_ChainsMultipleLinksAndRetainsAll()
    {
        DbManga manga = new() { MangaId = Guid.NewGuid(), Monitored = true };
        DbChapter chapter = new ChapterInfo(Guid.NewGuid(), "1", "https://example.com/1", "chapter-1").ToChapter(manga);

        ChapterInfo linkA = new(Guid.NewGuid(), "1", "https://a.example.com", "a");
        ChapterInfo linkB = new(Guid.NewGuid(), "1", "https://b.example.com", "b");

        chapter.CreateAndAddChapterDownloadLink(linkA).CreateAndAddChapterDownloadLink(linkB);

        Assert.NotNull(chapter.DownloadLinks);
        Assert.Equal(2, chapter.DownloadLinks!.Count);
        Assert.Contains(chapter.DownloadLinks, l => l.Identifier == "a");
        Assert.Contains(chapter.DownloadLinks, l => l.Identifier == "b");
    }

    [Fact]
    public void CreateAndAddChapterDownloadLink_InitializesCollectionWhenNull()
    {
        DbChapter chapter = new() { ChapterId = Guid.NewGuid(), MangaId = Guid.NewGuid(), Number = "1", DownloadLinks = null };
        ChapterInfo info = new(Guid.NewGuid(), "1", "https://example.com", "id");

        chapter.CreateAndAddChapterDownloadLink(info);

        Assert.NotNull(chapter.DownloadLinks);
        Assert.Single(chapter.DownloadLinks!);
    }

    [Fact]
    public void ToMangaInfo_ConvertsDbDownloadLinkToMangaInfo()
    {
        Guid extensionId = Guid.NewGuid();
        DbDownloadLink link = new()
        {
            DownloadLinkId = Guid.NewGuid(),
            DownloadExtension = extensionId,
            Identifier = "id",
            Series = "Series",
            Url = "https://example.com"
        };

        MangaInfo info = link.ToMangaInfo();

        Assert.Equal(extensionId, info.ExtensionIdentifier);
        Assert.Equal("https://example.com", info.Url);
        Assert.Equal("id", info.Identifier);
    }

    [Fact]
    public void ToMangaInfo_DefaultsUrlToEmptyStringWhenNull()
    {
        DbDownloadLink link = new()
        {
            DownloadLinkId = Guid.NewGuid(),
            DownloadExtension = Guid.NewGuid(),
            Identifier = "id",
            Series = "Series",
            Url = null
        };

        MangaInfo info = link.ToMangaInfo();

        Assert.Equal(string.Empty, info.Url);
    }
}
