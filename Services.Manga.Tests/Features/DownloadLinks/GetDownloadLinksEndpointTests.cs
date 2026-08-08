using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Services.Manga.Database;
using Services.Manga.Features.DownloadLinks;
using Services.Manga.Tests.Helpers;
using MangaDownloadLinkDto = Services.Manga.Entities.MangaDownloadLink;

namespace Services.Manga.Tests.Features.DownloadLinks;

public class GetDownloadLinksEndpointTests : TrangaTest
{
    [Fact]
    public async Task GetDownloadLinks_ReturnsAllLinks()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga manga, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);
        await TestDataBuilder.SeedMangaDownloadLink(context, manga, matched: true, ct: ct);
        await TestDataBuilder.SeedMangaDownloadLink(context, manga, matched: false, ct: ct);

        Results<Ok<MangaDownloadLinkDto[]>, InternalServerError> result = await GetDownloadLinksEndpoint.Handle(context, ct);

        MangaDownloadLinkDto[] links = Assert.IsType<Ok<MangaDownloadLinkDto[]>>(result.Result).Value!;
        Assert.Equal(2, links.Length);
    }

    [Fact]
    public async Task GetDownloadLinks_ReturnsEmptyWhenNoneExist()
    {
        await using MangaContext context = MangaContextFactory.Create();

        Results<Ok<MangaDownloadLinkDto[]>, InternalServerError> result = await GetDownloadLinksEndpoint.Handle(context, ct);

        MangaDownloadLinkDto[] links = Assert.IsType<Ok<MangaDownloadLinkDto[]>>(result.Result).Value!;
        Assert.Empty(links);
    }
}
