using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Services.Manga.Database;
using Services.Manga.Features.Manga;
using Services.Manga.Tests.Helpers;
using MetadataDto = Services.Manga.Entities.Metadata;

namespace Services.Manga.Tests.Features.Manga;

public class GetMangaMetadataEntriesEndpointTests : TrangaTest
{
    [Fact]
    public async Task GetMangaMetadataEntries_ReturnsAllRelatedEntries()
    {
        await using MangaContext context = MangaContextFactory.Create();
        DbManga manga = TestDataBuilder.NewManga();
        DbMetadata chosen = TestDataBuilder.NewMetadata("Chosen Series");
        DbMetadata alternative = TestDataBuilder.NewMetadata("Alternative Series");
        DbMangaMetadataEntries chosenEntry = new() { MangaId = manga.MangaId, MetadataId = chosen.MetadataId, Chosen = true, Manga = manga, Metadata = chosen };
        DbMangaMetadataEntries alternativeEntry = new() { MangaId = manga.MangaId, MetadataId = alternative.MetadataId, Chosen = false, Manga = manga, Metadata = alternative };

        await context.AddRangeAsync([manga, chosen, alternative, chosenEntry, alternativeEntry], ct);
        await context.SaveChangesAsync(ct);

        Results<Ok<MetadataDto[]>, NotFound> result = await GetMangaMetadataEntriesEndpoint.Handle(context, manga.MangaId, ct);

        MetadataDto[] entries = Assert.IsType<Ok<MetadataDto[]>>(result.Result).Value!;
        Assert.Equal(2, entries.Length);
        Assert.Contains(entries, e => e.Series == "Chosen Series");
        Assert.Contains(entries, e => e.Series == "Alternative Series");
    }

    [Fact]
    public async Task GetMangaMetadataEntries_ReturnsEmptyArrayWhenMangaHasNoEntries()
    {
        await using MangaContext context = MangaContextFactory.Create();

        Results<Ok<MetadataDto[]>, NotFound> result = await GetMangaMetadataEntriesEndpoint.Handle(context, Guid.NewGuid(), ct);

        MetadataDto[] entries = Assert.IsType<Ok<MetadataDto[]>>(result.Result).Value!;
        Assert.Empty(entries);
    }
}
