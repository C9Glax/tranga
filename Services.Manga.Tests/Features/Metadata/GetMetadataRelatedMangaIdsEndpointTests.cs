using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Services.Manga.Database;
using Services.Manga.Features.Metadata;
using Services.Manga.Tests.Helpers;

namespace Services.Manga.Tests.Features.Metadata;

public class GetMetadataRelatedMangaIdsEndpointTests : TrangaTest
{
    [Fact]
    public async Task GetMetadataRelatedMangaIds_ReturnsRelatedIds()
    {
        await using MangaContext context = MangaContextFactory.Create();
        DbMetadata metadata = TestDataBuilder.NewMetadata();
        DbManga mangaA = TestDataBuilder.NewManga();
        DbManga mangaB = TestDataBuilder.NewManga();
        DbMangaMetadataEntries entryA = new() { MangaId = mangaA.MangaId, MetadataId = metadata.MetadataId, Chosen = true, Manga = mangaA, Metadata = metadata };
        DbMangaMetadataEntries entryB = new() { MangaId = mangaB.MangaId, MetadataId = metadata.MetadataId, Chosen = false, Manga = mangaB, Metadata = metadata };
        await context.AddRangeAsync([metadata, mangaA, mangaB, entryA, entryB], ct);
        await context.SaveChangesAsync(ct);

        Results<Ok<Guid[]>, NotFound> result = await GetMetadataRelatedMangaIdsEndpoint.Handle(context, metadata.MetadataId, ct);

        Guid[] ids = Assert.IsType<Ok<Guid[]>>(result.Result).Value!;
        Assert.Equal(2, ids.Length);
        Assert.Contains(mangaA.MangaId, ids);
        Assert.Contains(mangaB.MangaId, ids);
    }

    [Fact]
    public async Task GetMetadataRelatedMangaIds_ReturnsEmptyForUnknownMetadataId()
    {
        await using MangaContext context = MangaContextFactory.Create();

        Results<Ok<Guid[]>, NotFound> result = await GetMetadataRelatedMangaIdsEndpoint.Handle(context, Guid.NewGuid(), ct);

        Guid[] ids = Assert.IsType<Ok<Guid[]>>(result.Result).Value!;
        Assert.Empty(ids);
    }
}
