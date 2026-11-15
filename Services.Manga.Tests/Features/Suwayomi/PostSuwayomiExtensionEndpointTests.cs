using Common.Tests;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Manga.Features.Suwayomi;
using Services.Manga.Tests.Helpers;

namespace Services.Manga.Tests.Features.Suwayomi;

public class PostSuwayomiExtensionEndpointTests : TrangaTest
{
    private static async Task<DbMangaDownloadLinks> SeedMangaLink(MangaContext context, DbManga manga, Guid extensionId, CancellationToken ct)
    {
        DbDownloadLink downloadLink = TestDataBuilder.NewDownloadLink(downloadExtension: extensionId);
        DbMangaDownloadLinks link = new()
        {
            MangaId = manga.MangaId,
            DownloadLinkId = downloadLink.DownloadLinkId,
            Matched = true,
            Priority = 0,
            Manga = manga,
            DownloadLink = downloadLink
        };
        await context.AddAsync(downloadLink, ct);
        await context.AddAsync(link, ct);
        await context.SaveChangesAsync(ct);
        return link;
    }

    private static async Task<DbChapterDownloadLink> SeedChapterLink(MangaContext context, DbChapter chapter, Guid extensionId, bool downloaded, CancellationToken ct)
    {
        Guid? fileId = null;
        if (downloaded)
        {
            DbFile file = new() { FileId = Guid.NewGuid(), Path = "/downloads", Name = $"{Guid.NewGuid():N}.cbz", MimeType = "application/zip" };
            await context.AddAsync(file, ct);
            fileId = file.FileId;
        }

        DbChapterDownloadLink link = new()
        {
            ChapterId = chapter.ChapterId,
            DownloadExtension = extensionId,
            Identifier = Guid.NewGuid().ToString(),
            Priority = 0,
            FileId = fileId,
            Chapter = chapter
        };
        await context.AddAsync(link, ct);
        await context.SaveChangesAsync(ct);
        return link;
    }

    [Fact]
    public async Task DeleteLinksOfRemovedExtensions_RemovesLinksOfRemovedExtensionOnly()
    {
        using MangaContext context = MangaContextFactory.Create();
        Guid removed = Guid.NewGuid();
        Guid kept = Guid.NewGuid();
        DbManga manga = TestDataBuilder.NewManga();
        await context.AddAsync(manga, ct);
        await context.SaveChangesAsync(ct);
        DbChapter chapter = await TestDataBuilder.SeedChapter(context, manga, ct: ct);

        DbMangaDownloadLinks removedMangaLink = await SeedMangaLink(context, manga, removed, ct);
        DbMangaDownloadLinks keptMangaLink = await SeedMangaLink(context, manga, kept, ct);
        await SeedChapterLink(context, chapter, removed, downloaded: false, ct);
        await SeedChapterLink(context, chapter, kept, downloaded: false, ct);

        int deleted = await PostSuwayomiExtensionEndpoint.DeleteLinksOfRemovedExtensions(context, [removed], ct);

        Assert.Equal(1, deleted);
        using MangaContext fresh = MangaContextFactory.Reopen(context);
        Assert.False(await fresh.DownloadLinks.AnyAsync(l => l.DownloadLinkId == removedMangaLink.DownloadLinkId, ct));
        Assert.False(await fresh.MangaDownloadLinks.AnyAsync(l => l.DownloadLinkId == removedMangaLink.DownloadLinkId, ct));
        Assert.True(await fresh.DownloadLinks.AnyAsync(l => l.DownloadLinkId == keptMangaLink.DownloadLinkId, ct));
        Assert.True(await fresh.MangaDownloadLinks.AnyAsync(l => l.DownloadLinkId == keptMangaLink.DownloadLinkId, ct));
        Assert.False(await fresh.ChapterDownloadLinks.AnyAsync(l => l.DownloadExtension == removed, ct));
        Assert.True(await fresh.ChapterDownloadLinks.AnyAsync(l => l.DownloadExtension == kept, ct));
    }

    [Fact]
    public async Task DeleteLinksOfRemovedExtensions_KeepsMangaChaptersAndDownloadedChapterLinks()
    {
        using MangaContext context = MangaContextFactory.Create();
        Guid removed = Guid.NewGuid();
        DbManga manga = TestDataBuilder.NewManga();
        await context.AddAsync(manga, ct);
        await context.SaveChangesAsync(ct);
        DbChapter chapter = await TestDataBuilder.SeedChapter(context, manga, ct: ct);
        DbChapterDownloadLink downloaded = await SeedChapterLink(context, chapter, removed, downloaded: true, ct);
        await SeedMangaLink(context, manga, removed, ct);

        await PostSuwayomiExtensionEndpoint.DeleteLinksOfRemovedExtensions(context, [removed], ct);

        using MangaContext fresh = MangaContextFactory.Reopen(context);
        Assert.True(await fresh.Mangas.AnyAsync(m => m.MangaId == manga.MangaId, ct));
        Assert.True(await fresh.Chapters.AnyAsync(c => c.ChapterId == chapter.ChapterId, ct));
        Assert.True(await fresh.Files.AnyAsync(f => f.FileId == downloaded.FileId, ct));
        Assert.True(await fresh.ChapterDownloadLinks.AnyAsync(l => l.ChapterId == chapter.ChapterId && l.FileId == downloaded.FileId, ct));
        Assert.False(await fresh.MangaDownloadLinks.AnyAsync(l => l.MangaId == manga.MangaId, ct));
    }

    [Fact]
    public async Task DeleteLinksOfRemovedExtensions_NoRemovedExtensions_DeletesNothing()
    {
        using MangaContext context = MangaContextFactory.Create();
        DbManga manga = TestDataBuilder.NewManga();
        await context.AddAsync(manga, ct);
        await context.SaveChangesAsync(ct);
        await SeedMangaLink(context, manga, Guid.NewGuid(), ct);

        int deleted = await PostSuwayomiExtensionEndpoint.DeleteLinksOfRemovedExtensions(context, [], ct);

        Assert.Equal(0, deleted);
        Assert.Equal(1, await context.DownloadLinks.CountAsync(ct));
    }
}
