using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Services.Manga.Database;
using Services.Manga.Features.Manga;
using Services.Manga.Tests.Helpers;
using MangaDownloadLinkDto = Services.Manga.Entities.MangaDownloadLink;

namespace Services.Manga.Tests.Features.Manga;

public class GetMangaDownloadLinksEndpointTests : TrangaTest
{
    [Fact]
    public async Task GetMangaDownloadLinks_ReturnsAllMatchedLinks()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga manga, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);
        DbMangaDownloadLinks matched = await TestDataBuilder.SeedMangaDownloadLink(context, manga, matched: true, ct: ct);

        Results<Ok<MangaDownloadLinkDto[]>, NotFound> result = await GetMangaDownloadLinksEndpoint.Handle(context, manga.MangaId, ct);

        MangaDownloadLinkDto[] links = Assert.IsType<Ok<MangaDownloadLinkDto[]>>(result.Result).Value!;
        Assert.Single(links);
        Assert.Equal(matched.DownloadLinkId, links[0].DownloadId);
    }

    [Fact]
    public async Task GetMangaDownloadLinks_ExcludesUnmatchedLinks()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga manga, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);
        await TestDataBuilder.SeedMangaDownloadLink(context, manga, matched: false, ct: ct);

        Results<Ok<MangaDownloadLinkDto[]>, NotFound> result = await GetMangaDownloadLinksEndpoint.Handle(context, manga.MangaId, ct);

        MangaDownloadLinkDto[] links = Assert.IsType<Ok<MangaDownloadLinkDto[]>>(result.Result).Value!;
        Assert.Empty(links);
    }

    [Fact]
    public async Task GetMangaDownloadLinks_ReturnsEmptyForUnknownManga()
    {
        await using MangaContext context = MangaContextFactory.Create();

        Results<Ok<MangaDownloadLinkDto[]>, NotFound> result = await GetMangaDownloadLinksEndpoint.Handle(context, Guid.NewGuid(), ct);

        MangaDownloadLinkDto[] links = Assert.IsType<Ok<MangaDownloadLinkDto[]>>(result.Result).Value!;
        Assert.Empty(links);
    }
}
