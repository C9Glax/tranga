using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Services.Manga.Database;
using Services.Manga.Entities;
using Services.Manga.Features.DownloadLinks;
using Services.Manga.Tests.Helpers;

namespace Services.Manga.Tests.Features.DownloadLinks;

public class GetDownloadLinkEndpointTests : TrangaTest
{
    [Fact]
    public async Task GetDownloadLink_ReturnsSpecificLinkById()
    {
        await using MangaContext context = MangaContextFactory.Create();
        DbDownloadLink link = TestDataBuilder.NewDownloadLink("One Piece");
        await context.AddAsync(link, ct);
        await context.SaveChangesAsync(ct);

        Results<Ok<DownloadLink>, NotFound> result = await GetDownloadLinkEndpoint.Handle(context, link.DownloadLinkId, ct);

        DownloadLink dto = Assert.IsType<Ok<DownloadLink>>(result.Result).Value!;
        Assert.Equal(link.DownloadLinkId, dto.DownloadId);
        Assert.Equal("One Piece", dto.Series);
    }

    [Fact]
    public async Task GetDownloadLink_Returns404ForUnknownId()
    {
        await using MangaContext context = MangaContextFactory.Create();

        Results<Ok<DownloadLink>, NotFound> result = await GetDownloadLinkEndpoint.Handle(context, Guid.NewGuid(), ct);

        Assert.IsType<NotFound>(result.Result);
    }
}
