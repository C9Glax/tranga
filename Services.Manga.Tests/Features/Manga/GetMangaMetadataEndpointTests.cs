using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Services.Manga.Database;
using Services.Manga.Features.Manga;
using Services.Manga.Tests.Helpers;
using MetadataDto = Services.Manga.Entities.Metadata;

namespace Services.Manga.Tests.Features.Manga;

public class GetMangaMetadataEndpointTests : TrangaTest
{
    [Fact]
    public async Task GetMangaMetadata_ReturnsChosenMetadataForManga()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga manga, DbMetadata metadata, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, series: "One Piece", ct: ct);

        Results<Ok<MetadataDto>, NotFound> result = await GetMangaMetadataEndpoint.Handle(context, manga.MangaId, ct);

        MetadataDto dto = Assert.IsType<Ok<MetadataDto>>(result.Result).Value!;
        Assert.Equal(metadata.MetadataId, dto.MetadataId);
        Assert.Equal("One Piece", dto.Series);
    }

    [Fact]
    public async Task GetMangaMetadata_Returns404ForUnknownManga()
    {
        await using MangaContext context = MangaContextFactory.Create();

        Results<Ok<MetadataDto>, NotFound> result = await GetMangaMetadataEndpoint.Handle(context, Guid.NewGuid(), ct);

        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task GetMangaMetadata_Returns404WhenNoEntryIsChosen()
    {
        await using MangaContext context = MangaContextFactory.Create();
        DbManga manga = TestDataBuilder.NewManga();
        DbMetadata metadata = TestDataBuilder.NewMetadata();
        DbMangaMetadataEntries entry = new()
        {
            MangaId = manga.MangaId,
            MetadataId = metadata.MetadataId,
            Chosen = false,
            Manga = manga,
            Metadata = metadata
        };
        await context.AddAsync(manga, ct);
        await context.AddAsync(metadata, ct);
        await context.AddAsync(entry, ct);
        await context.SaveChangesAsync(ct);

        Results<Ok<MetadataDto>, NotFound> result = await GetMangaMetadataEndpoint.Handle(context, manga.MangaId, ct);

        Assert.IsType<NotFound>(result.Result);
    }
}
