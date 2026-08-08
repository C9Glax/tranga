using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Services.Manga.Database;
using Services.Manga.Features.Metadata;
using Services.Manga.Tests.Helpers;
using MetadataDto = Services.Manga.Entities.Metadata;

namespace Services.Manga.Tests.Features.Metadata;

public class GetMetadataEntryEndpointTests : TrangaTest
{
    [Fact]
    public async Task GetMetadataEntry_ReturnsSpecificEntryById()
    {
        await using MangaContext context = MangaContextFactory.Create();
        DbMetadata metadata = TestDataBuilder.NewMetadata("One Piece");
        await context.AddAsync(metadata, ct);
        await context.SaveChangesAsync(ct);

        Results<Ok<MetadataDto>, NotFound> result = await GetMetadataEntryEndpoint.Handle(context, metadata.MetadataId, ct);

        MetadataDto dto = Assert.IsType<Ok<MetadataDto>>(result.Result).Value!;
        Assert.Equal(metadata.MetadataId, dto.MetadataId);
        Assert.Equal("One Piece", dto.Series);
    }

    [Fact]
    public async Task GetMetadataEntry_Returns404ForUnknownId()
    {
        await using MangaContext context = MangaContextFactory.Create();

        Results<Ok<MetadataDto>, NotFound> result = await GetMetadataEntryEndpoint.Handle(context, Guid.NewGuid(), ct);

        Assert.IsType<NotFound>(result.Result);
    }
}
