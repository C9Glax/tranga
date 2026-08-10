using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Services.Manga.Database;
using Services.Manga.Features.Manga;
using Services.Manga.Tests.Helpers;
using MangaDto = Services.Manga.Entities.Manga;

namespace Services.Manga.Tests.Features.Manga;

public class GetMangaListEndpointTests : TrangaTest
{
    [Fact]
    public async Task GetMangaList_ReturnsAllMangaWithChosenMetadata()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga manga1, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, series: "Series A", ct: ct);
        (DbManga manga2, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, series: "Series B", ct: ct);

        Results<Ok<MangaDto[]>, InternalServerError> result = await GetMangaListEndpoint.Handle(context, ct);

        MangaDto[] mangas = Assert.IsType<Ok<MangaDto[]>>(result.Result).Value!;
        Assert.Equal(2, mangas.Length);
        Assert.Contains(mangas, m => m.MangaId == manga1.MangaId);
        Assert.Contains(mangas, m => m.MangaId == manga2.MangaId);
    }

    [Fact]
    public async Task GetMangaList_ReturnsMangaOrderedAlphabeticallyBySeries()
    {
        await using MangaContext context = MangaContextFactory.Create();
        await TestDataBuilder.SeedMangaWithChosenMetadata(context, series: "Zeta", ct: ct);
        await TestDataBuilder.SeedMangaWithChosenMetadata(context, series: "Alpha", ct: ct);
        await TestDataBuilder.SeedMangaWithChosenMetadata(context, series: "Mu", ct: ct);

        Results<Ok<MangaDto[]>, InternalServerError> result = await GetMangaListEndpoint.Handle(context, ct);

        MangaDto[] mangas = Assert.IsType<Ok<MangaDto[]>>(result.Result).Value!;
        Assert.Equal(["Alpha", "Mu", "Zeta"], mangas.Select(m => m.MetadataEntry!.Series));
    }

    [Fact]
    public async Task GetMangaList_ReturnsEmptyWhenNoneExist()
    {
        await using MangaContext context = MangaContextFactory.Create();

        Results<Ok<MangaDto[]>, InternalServerError> result = await GetMangaListEndpoint.Handle(context, ct);

        MangaDto[] mangas = Assert.IsType<Ok<MangaDto[]>>(result.Result).Value!;
        Assert.Empty(mangas);
    }

    [Fact]
    public async Task GetMangaList_ExcludesEntriesWithoutChosenMetadata()
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

        Results<Ok<MangaDto[]>, InternalServerError> result = await GetMangaListEndpoint.Handle(context, ct);

        MangaDto[] mangas = Assert.IsType<Ok<MangaDto[]>>(result.Result).Value!;
        Assert.Empty(mangas);
    }
}
