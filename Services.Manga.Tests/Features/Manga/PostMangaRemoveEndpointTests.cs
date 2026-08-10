using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Manga.Features.Manga;
using Services.Manga.Tests.Helpers;

namespace Services.Manga.Tests.Features.Manga;

public class PostMangaRemoveEndpointTests : TrangaTest
{
    [Fact]
    public async Task Remove_UnmonitorsMangaAndUnchoosesMetadata()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga manga, DbMetadata metadata, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, monitored: true, ct: ct);

        Results<Ok, NotFound> result = await PostMangaRemoveEndpoint.Handle(context, manga.MangaId, ct);

        Assert.IsType<Ok>(result.Result);

        // AsNoTracking: Chosen is cleared via a bulk ExecuteUpdateAsync, which bypasses the
        // change tracker - a tracking query here would return the stale in-memory value.
        DbManga persistedManga = await context.Mangas.AsNoTracking().SingleAsync(m => m.MangaId == manga.MangaId, ct);
        DbMangaMetadataEntries persistedEntry = await context.MangaMetadataEntries.AsNoTracking()
            .SingleAsync(e => e.MangaId == manga.MangaId && e.MetadataId == metadata.MetadataId, ct);

        Assert.False(persistedManga.Monitored);
        Assert.False(persistedEntry.Chosen);
    }

    [Fact]
    public async Task Remove_Returns404ForUnknownManga()
    {
        await using MangaContext context = MangaContextFactory.Create();

        Results<Ok, NotFound> result = await PostMangaRemoveEndpoint.Handle(context, Guid.NewGuid(), ct);

        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task Remove_IsIdempotent()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga manga, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, monitored: true, ct: ct);

        Results<Ok, NotFound> first = await PostMangaRemoveEndpoint.Handle(context, manga.MangaId, ct);
        Results<Ok, NotFound> second = await PostMangaRemoveEndpoint.Handle(context, manga.MangaId, ct);

        Assert.IsType<Ok>(first.Result);
        Assert.IsType<Ok>(second.Result);
    }
}
