using Services.Manga.Database;
using Services.Manga.Entities;
using Services.Manga.Helpers;

namespace Services.Manga.Tests.Helpers;

public class DownloadLinkDTOHelperTests
{
    [Fact]
    public void ToDTO_ConvertsDbMangaDownloadLinksToMangaDownloadLink()
    {
        Guid extensionId = Guid.NewGuid();
        DbFile cover = new() { FileId = Guid.NewGuid(), Path = "/covers", Name = "cover.jpg", MimeType = "image/jpeg" };
        DbDownloadLink link = new()
        {
            DownloadLinkId = Guid.NewGuid(),
            DownloadExtension = extensionId,
            Identifier = "id",
            Series = "Series",
            Summary = "Summary",
            Language = "en",
            Url = "https://example.com",
            CoverId = cover.FileId,
            Cover = cover,
            NSFW = false
        };
        DbManga manga = new() { MangaId = Guid.NewGuid(), Monitored = true };
        DbMangaDownloadLinks entry = new()
        {
            MangaId = manga.MangaId,
            DownloadLinkId = link.DownloadLinkId,
            Matched = true,
            Priority = 3,
            Manga = manga,
            DownloadLink = link
        };

        MangaDownloadLink dto = entry.ToDTO();

        Assert.Equal(manga.MangaId, dto.MangaId);
        Assert.Equal(link.DownloadLinkId, dto.DownloadId);
        Assert.Equal(extensionId, dto.DownloadExtensionId);
        Assert.True(dto.Matched);
        Assert.Equal(3, dto.Priority);
        Assert.Equal("Series", dto.Series);
        Assert.Equal("Summary", dto.Summary);
        Assert.Equal("en", dto.Language);
        Assert.Equal(cover.FileId, dto.CoverId);
    }

    [Fact]
    public void ToDTO_FallsBackToCoverNavigationWhenCoverIdIsNull()
    {
        DbFile cover = new() { FileId = Guid.NewGuid(), Path = "/covers", Name = "cover.jpg", MimeType = "image/jpeg" };
        DbDownloadLink link = new()
        {
            DownloadLinkId = Guid.NewGuid(),
            DownloadExtension = Guid.NewGuid(),
            Identifier = "id",
            Series = "Series",
            CoverId = null,
            Cover = cover
        };
        DbManga manga = new() { MangaId = Guid.NewGuid(), Monitored = true };
        DbMangaDownloadLinks entry = new()
        {
            MangaId = manga.MangaId,
            DownloadLinkId = link.DownloadLinkId,
            Matched = false,
            Priority = 0,
            Manga = manga,
            DownloadLink = link
        };

        MangaDownloadLink dto = entry.ToDTO();

        Assert.Equal(cover.FileId, dto.CoverId);
    }

    [Fact]
    public void ToDTO_ConvertsDbDownloadLinkToDownloadLink()
    {
        DbDownloadLink link = new()
        {
            DownloadLinkId = Guid.NewGuid(),
            DownloadExtension = Guid.NewGuid(),
            Identifier = "id",
            Series = "Series",
            Summary = "Summary",
            Language = "en",
            Url = "https://example.com",
            NSFW = true
        };

        DownloadLink dto = link.ToDTO();

        Assert.Equal(link.DownloadLinkId, dto.DownloadId);
        Assert.Equal(link.DownloadExtension, dto.DownloadExtensionId);
        Assert.Equal("Series", dto.Series);
        Assert.Equal("Summary", dto.Summary);
        Assert.Equal("en", dto.Language);
        Assert.True(dto.NSFW);
    }

    [Fact]
    public void ToDTO_DbDownloadLinkFallsBackToCoverNavigationWhenCoverIdIsNull()
    {
        DbFile cover = new() { FileId = Guid.NewGuid(), Path = "/covers", Name = "cover.jpg", MimeType = "image/jpeg" };
        DbDownloadLink link = new()
        {
            DownloadLinkId = Guid.NewGuid(),
            DownloadExtension = Guid.NewGuid(),
            Identifier = "id",
            Series = "Series",
            CoverId = null,
            Cover = cover
        };

        DownloadLink dto = link.ToDTO();

        Assert.Equal(cover.FileId, dto.CoverId);
    }
}
