using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Services.Manga.Database;
using Services.Manga.Features.Manga;
using Services.Manga.Tests.Helpers;
using MangaDto = Services.Manga.Entities.Manga;

namespace Services.Manga.Tests.Features.Manga;

public class GetMangaEndpointTests : TrangaTest
{
    [Fact]
    public async Task GetManga_ReturnsSpecificMangaById()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga manga, DbMetadata metadata, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, series: "One Piece", ct: ct);

        Results<Ok<MangaDto>, NotFound> result = await GetMangaEndpoint.Handle(context, manga.MangaId, ct);

        MangaDto dto = Assert.IsType<Ok<MangaDto>>(result.Result).Value!;
        Assert.Equal(manga.MangaId, dto.MangaId);
        Assert.NotNull(dto.MetadataEntry);
        Assert.Equal(metadata.MetadataId, dto.MetadataEntry!.MetadataId);
        Assert.Equal("One Piece", dto.MetadataEntry.Series);
    }

    [Fact]
    public async Task GetManga_Returns404ForUnknownId()
    {
        await using MangaContext context = MangaContextFactory.Create();

        Results<Ok<MangaDto>, NotFound> result = await GetMangaEndpoint.Handle(context, Guid.NewGuid(), ct);

        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task GetManga_Returns404WhenNoMetadataEntryIsChosen()
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

        Results<Ok<MangaDto>, NotFound> result = await GetMangaEndpoint.Handle(context, manga.MangaId, ct);

        Assert.IsType<NotFound>(result.Result);
    }
}
