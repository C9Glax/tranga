using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Services.Manga.Database;
using Services.Manga.Features.Metadata;
using Services.Manga.Tests.Helpers;
using MangaDto = Services.Manga.Entities.Manga;

namespace Services.Manga.Tests.Features.Metadata;

public class GetMetadataMangaEndpointTests : TrangaTest
{
    [Fact]
    public async Task GetMetadataManga_ReturnsLinkedManga()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga manga, DbMetadata metadata, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);

        Results<Ok<MangaDto>, NotFound> result = await GetMetadataMangaEndpoint.Handle(context, metadata.MetadataId, ct);

        MangaDto dto = Assert.IsType<Ok<MangaDto>>(result.Result).Value!;
        Assert.Equal(manga.MangaId, dto.MangaId);
    }

    [Fact]
    public async Task GetMetadataManga_Returns404WhenNotChosenForAnyManga()
    {
        await using MangaContext context = MangaContextFactory.Create();
        DbManga manga = TestDataBuilder.NewManga();
        DbMetadata metadata = TestDataBuilder.NewMetadata();
        DbMangaMetadataEntries entry = new() { MangaId = manga.MangaId, MetadataId = metadata.MetadataId, Chosen = false, Manga = manga, Metadata = metadata };
        await context.AddRangeAsync([manga, metadata, entry], ct);
        await context.SaveChangesAsync(ct);

        Results<Ok<MangaDto>, NotFound> result = await GetMetadataMangaEndpoint.Handle(context, metadata.MetadataId, ct);

        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task GetMetadataManga_Returns404ForUnknownMetadataId()
    {
        await using MangaContext context = MangaContextFactory.Create();

        Results<Ok<MangaDto>, NotFound> result = await GetMetadataMangaEndpoint.Handle(context, Guid.NewGuid(), ct);

        Assert.IsType<NotFound>(result.Result);
    }
}
