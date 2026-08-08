using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Services.Manga.Database;
using Services.Manga.Features.Manga.Search;
using Services.Manga.Tests.Helpers;
using MangaDownloadLinkDto = Services.Manga.Entities.MangaDownloadLink;

namespace Services.Manga.Tests.Features.Manga.Search;

public class PostSearchMangaDownloadLinksEndpointTests : TrangaTest
{
    // NOTE: unlike PostSearchMangaEndpoint, this endpoint always searches every registered
    // download extension (Extensions.DownloadExtensionsCollection.SearchAll) with no way to filter
    // the set down to zero - so only the 404 path (which returns before any extension is searched)
    // can be exercised here without making a real HTTP call to a download extension.

    [Fact]
    public async Task PostSearchMangaDownloadLinks_Returns404ForUnknownManga()
    {
        await using MangaContext context = MangaContextFactory.Create();

        Results<Ok<MangaDownloadLinkDto[]>, NotFound, InternalServerError> result =
            await PostSearchMangaDownloadLinksEndpoint.Handle(context, Guid.NewGuid(), ct);

        Assert.IsType<NotFound>(result.Result);
    }
}
