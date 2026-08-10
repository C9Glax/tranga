using Common.Tests;
using Extensions.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Services.Manga.Database;
using Services.Manga.Features.Manga;
using Services.Manga.Tests.Helpers;
using MangaDownloadLinkDto = Services.Manga.Entities.MangaDownloadLink;

namespace Services.Manga.Tests.Features.Manga;

public class PostMangaDownloadLinkEndpointTests : TrangaTest
{
    // NOTE: like PostSearchMangaDownloadLinksEndpointTests, the success path (a real GetChapters
    // call resolving) can't be exercised here without a live extension call - DownloadExtensionsCollection
    // has no mocking seam. Every case below returns before that network call is reached.

    [Fact]
    public async Task PostMangaDownloadLink_Returns404ForUnknownManga()
    {
        await using MangaContext context = MangaContextFactory.Create();

        Results<Ok<MangaDownloadLinkDto>, NotFound, BadRequest<string>> result = await PostMangaDownloadLinkEndpoint.Handle(
            context, Guid.NewGuid(), new PostMangaDownloadLinkEndpoint.PostMangaDownloadLinkRequest(Guid.NewGuid(), "https://example.com/foo"), ct);

        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task PostMangaDownloadLink_Returns400ForUnknownExtension()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga manga, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);

        Results<Ok<MangaDownloadLinkDto>, NotFound, BadRequest<string>> result = await PostMangaDownloadLinkEndpoint.Handle(
            context, manga.MangaId, new PostMangaDownloadLinkEndpoint.PostMangaDownloadLinkRequest(Guid.NewGuid(), "https://example.com/foo"), ct);

        Assert.IsType<BadRequest<string>>(result.Result);
    }

    [Fact]
    public async Task PostMangaDownloadLink_Returns400ForUrlNotMatchingExtension()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga manga, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);

        Results<Ok<MangaDownloadLinkDto>, NotFound, BadRequest<string>> result = await PostMangaDownloadLinkEndpoint.Handle(
            context, manga.MangaId,
            new PostMangaDownloadLinkEndpoint.PostMangaDownloadLinkRequest(new MangaDex().Identifier, "https://example.com/foo"), ct);

        Assert.IsType<BadRequest<string>>(result.Result);
    }

    [Fact]
    public async Task PostMangaDownloadLink_Returns400ForDuplicateLink()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga manga, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);

        Guid extensionId = new MangaDex().Identifier;
        Guid seriesGuid = Guid.NewGuid();

        DbDownloadLink downloadLink = new()
        {
            DownloadLinkId = Guid.NewGuid(),
            DownloadExtension = extensionId,
            Identifier = seriesGuid.ToString(),
            Series = "Series",
            Url = $"https://mangadex.org/title/{seriesGuid}"
        };
        DbMangaDownloadLinks link = new()
        {
            MangaId = manga.MangaId,
            DownloadLinkId = downloadLink.DownloadLinkId,
            Matched = false,
            Priority = 0,
            Manga = manga,
            DownloadLink = downloadLink
        };
        await context.AddAsync(downloadLink, ct);
        await context.AddAsync(link, ct);
        await context.SaveChangesAsync(ct);

        Results<Ok<MangaDownloadLinkDto>, NotFound, BadRequest<string>> result = await PostMangaDownloadLinkEndpoint.Handle(
            context, manga.MangaId,
            new PostMangaDownloadLinkEndpoint.PostMangaDownloadLinkRequest(extensionId, $"https://mangadex.org/title/{seriesGuid}"), ct);

        Assert.IsType<BadRequest<string>>(result.Result);
    }
}
