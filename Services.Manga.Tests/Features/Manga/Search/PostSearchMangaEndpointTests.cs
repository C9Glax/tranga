using Common.Datatypes;
using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Services.Manga.Database;
using Services.Manga.Features.Manga.Search;
using Services.Manga.Tests.Helpers;
using MetadataDto = Services.Manga.Entities.Metadata;

namespace Services.Manga.Tests.Features.Manga.Search;

public class PostSearchMangaEndpointTests : TrangaTest
{
    [Fact]
    public async Task PostSearchManga_Rejects400WhenQueryHasNoUsableCriteria()
    {
        await using MangaContext context = MangaContextFactory.Create();
        PostSearchMangaEndpoint.PostSearchMangaRequest request = new(new SearchQuery(), null);

        Results<Ok<MetadataDto[]>, BadRequest, InternalServerError> result = await PostSearchMangaEndpoint.Handle(context, request, ct);

        Assert.IsType<BadRequest>(result.Result);
    }

    [Fact]
    public async Task PostSearchManga_SearchesRequestedMetadataExtensions()
    {
        await using MangaContext context = MangaContextFactory.Create();
        // A random, non-registered extension ID filters the extension set down to empty,
        // so the search runs (and returns) without making a real HTTP call to any extension.
        PostSearchMangaEndpoint.PostSearchMangaRequest request = new(
            new SearchQuery(Title: "One Piece"), [Guid.NewGuid()]);

        Results<Ok<MetadataDto[]>, BadRequest, InternalServerError> result = await PostSearchMangaEndpoint.Handle(context, request, ct);

        MetadataDto[] results = Assert.IsType<Ok<MetadataDto[]>>(result.Result).Value!;
        Assert.Empty(results);
    }
}
