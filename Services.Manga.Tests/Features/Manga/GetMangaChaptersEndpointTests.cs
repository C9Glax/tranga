using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Services.Manga.Database;
using Services.Manga.Features.Manga;
using Services.Manga.Tests.Helpers;
using MangaChapterDto = Services.Manga.Entities.MangaChapter;

namespace Services.Manga.Tests.Features.Manga;

public class GetMangaChaptersEndpointTests : TrangaTest
{
    [Fact]
    public async Task GetMangaChapters_ReturnsAllChaptersForManga()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga manga, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);
        await TestDataBuilder.SeedChapter(context, manga, number: "1", ct: ct);
        await TestDataBuilder.SeedChapter(context, manga, number: "2", ct: ct);

        Ok<MangaChapterDto[]> result = await GetMangaChaptersEndpoint.Handle(context, manga.MangaId, ct);

        MangaChapterDto[] chapters = result.Value!;
        Assert.Equal(2, chapters.Length);
    }

    [Fact]
    public async Task GetMangaChapters_MarksChapterDownloadedWhenLinkHasFileId()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga manga, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);
        DbChapter chapter = await TestDataBuilder.SeedChapter(context, manga, ct: ct);
        await TestDataBuilder.SeedChapterDownloadLink(context, chapter, downloaded: true, ct: ct);

        Ok<MangaChapterDto[]> result = await GetMangaChaptersEndpoint.Handle(context, manga.MangaId, ct);

        MangaChapterDto dto = Assert.Single(result.Value!);
        Assert.True(dto.IsDownloaded);
    }

    [Fact]
    public async Task GetMangaChapters_NotDownloadedWhenLinkHasNoFileId()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga manga, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);
        DbChapter chapter = await TestDataBuilder.SeedChapter(context, manga, ct: ct);
        await TestDataBuilder.SeedChapterDownloadLink(context, chapter, downloaded: false, ct: ct);

        Ok<MangaChapterDto[]> result = await GetMangaChaptersEndpoint.Handle(context, manga.MangaId, ct);

        MangaChapterDto dto = Assert.Single(result.Value!);
        Assert.False(dto.IsDownloaded);
    }

    [Fact]
    public async Task GetMangaChapters_NotDownloadedWhenNoLinks()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga manga, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);
        await TestDataBuilder.SeedChapter(context, manga, ct: ct);

        Ok<MangaChapterDto[]> result = await GetMangaChaptersEndpoint.Handle(context, manga.MangaId, ct);

        MangaChapterDto dto = Assert.Single(result.Value!);
        Assert.False(dto.IsDownloaded);
    }

    [Fact]
    public async Task GetMangaChapters_ReturnsEmptyForUnknownManga()
    {
        await using MangaContext context = MangaContextFactory.Create();

        Ok<MangaChapterDto[]> result = await GetMangaChaptersEndpoint.Handle(context, Guid.NewGuid(), ct);

        Assert.Empty(result.Value!);
    }
}
