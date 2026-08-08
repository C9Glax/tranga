using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Manga.Features.Manga;
using Services.Manga.Tests.Helpers;

namespace Services.Manga.Tests.Features.Manga;

public class PatchMangaMetadataEntryChosenEndpointTests : TrangaTest
{
    [Fact]
    public async Task PatchMangaMetadata_SetsChosenEntryAndUnsetsPreviousOne()
    {
        await using MangaContext context = MangaContextFactory.Create();
        DbManga manga = TestDataBuilder.NewManga();
        DbMetadata previouslyChosen = TestDataBuilder.NewMetadata("Previously Chosen");
        DbMetadata toChoose = TestDataBuilder.NewMetadata("To Choose");
        DbMangaMetadataEntries previousEntry = new() { MangaId = manga.MangaId, MetadataId = previouslyChosen.MetadataId, Chosen = true, Manga = manga, Metadata = previouslyChosen };
        DbMangaMetadataEntries newEntry = new() { MangaId = manga.MangaId, MetadataId = toChoose.MetadataId, Chosen = false, Manga = manga, Metadata = toChoose };
        await context.AddRangeAsync([manga, previouslyChosen, toChoose, previousEntry, newEntry], ct);
        await context.SaveChangesAsync(ct);

        Results<Ok, NotFound> result = await PatchMangaMetadataEntryChosenEndpoint.Handle(context, manga.MangaId, toChoose.MetadataId, ct);

        Assert.IsType<Ok>(result.Result);

        // AsNoTracking: the endpoint changes `previousEntry`'s Chosen flag via a bulk
        // ExecuteUpdateAsync, which bypasses the change tracker - a tracking query here would
        // return the stale in-memory value seeded above instead of what was actually persisted.
        List<DbMangaMetadataEntries> entries = await context.MangaMetadataEntries.AsNoTracking().Where(e => e.MangaId == manga.MangaId).ToListAsync(ct);
        Assert.True(entries.Single(e => e.MetadataId == toChoose.MetadataId).Chosen);
        Assert.False(entries.Single(e => e.MetadataId == previouslyChosen.MetadataId).Chosen);
    }

    [Fact]
    public async Task PatchMangaMetadata_Returns404ForUnknownManga()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (_, DbMetadata metadata, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);

        Results<Ok, NotFound> result = await PatchMangaMetadataEntryChosenEndpoint.Handle(context, Guid.NewGuid(), metadata.MetadataId, ct);

        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task PatchMangaMetadata_Returns404ForUnknownMetadataId()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga manga, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);

        Results<Ok, NotFound> result = await PatchMangaMetadataEntryChosenEndpoint.Handle(context, manga.MangaId, Guid.NewGuid(), ct);

        Assert.IsType<NotFound>(result.Result);
    }
}
