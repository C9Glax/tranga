using Common.Services.Events;
using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using RabbitMQ.Client;
using Services.Manga.Database;
using Services.Manga.Features.Manga;
using Services.Manga.Tests.Helpers;
using MangaDto = Services.Manga.Entities.Manga;
using MetadataDto = Services.Manga.Entities.Metadata;
using MangaDownloadLinkDto = Services.Manga.Entities.MangaDownloadLink;

namespace Services.Manga.Tests.Integration;

/// <summary>
/// End-to-end flows across multiple endpoint handlers sharing one <see cref="MangaContext"/>.
/// These seed Manga/Metadata/DownloadLink data directly rather than going through the
/// PostSearchManga*/download-search endpoints, since those call live metadata/download
/// extensions over HTTP - not appropriate for a hermetic test suite. See
/// PostSearchMangaEndpointTests/PostSearchMangaDownloadLinksEndpointTests for what is
/// covered of that path without a live network call.
/// </summary>
public class MangaLifecycleIntegrationTests : TrangaTest
{
    private static EventPublisher CreateEventPublisher()
    {
        Mock<IChannel> mockChannel = new();
        mockChannel.Setup(c => c.IsOpen).Returns(true);
        return new EventPublisher(mockChannel.Object);
    }

    [Fact]
    public async Task Integration_MangaLifecycle_MetadataSelectionAndDownloadLinksAreConsistentAcrossEndpoints()
    {
        await using MangaContext context = MangaContextFactory.Create();

        // Manga starts with two competing Metadata-Entries, neither chosen yet.
        DbManga manga = TestDataBuilder.NewManga();
        DbMetadata candidateA = TestDataBuilder.NewMetadata("Candidate A");
        DbMetadata candidateB = TestDataBuilder.NewMetadata("Candidate B");
        DbMangaMetadataEntries entryA = new() { MangaId = manga.MangaId, MetadataId = candidateA.MetadataId, Chosen = false, Manga = manga, Metadata = candidateA };
        DbMangaMetadataEntries entryB = new() { MangaId = manga.MangaId, MetadataId = candidateB.MetadataId, Chosen = false, Manga = manga, Metadata = candidateB };
        await context.AddRangeAsync([manga, candidateA, candidateB, entryA, entryB], ct);
        await context.SaveChangesAsync(ct);

        // Before a candidate is chosen, the Manga has no "source of truth" yet.
        Assert.IsType<NotFound>((await GetMangaEndpoint.Handle(context, manga.MangaId, ct)).Result);

        // Both candidates are visible as related entries regardless of which (if any) is chosen.
        MetadataDto[] related = Assert.IsType<Ok<MetadataDto[]>>(
            (await GetMangaMetadataEntriesEndpoint.Handle(context, manga.MangaId, ct)).Result).Value!;
        Assert.Equal(2, related.Length);

        // Choose candidate A.
        Assert.IsType<Ok>((await PatchMangaMetadataEntryChosenEndpoint.Handle(context, CreateEventPublisher(), manga.MangaId, candidateA.MetadataId, ct)).Result);

        MangaDto manga1 = Assert.IsType<Ok<MangaDto>>((await GetMangaEndpoint.Handle(context, manga.MangaId, ct)).Result).Value!;
        Assert.Equal("Candidate A", manga1.MetadataEntry!.Series);

        // Add a Download-Link and match it - GetMangaDownloadLinks should reflect it immediately.
        DbMangaDownloadLinks link = await TestDataBuilder.SeedMangaDownloadLink(context, manga, matched: false, priority: 0, ct: ct);
        Assert.Empty(Assert.IsType<Ok<MangaDownloadLinkDto[]>>(
            (await GetMangaDownloadLinksEndpoint.Handle(context, manga.MangaId, ct)).Result).Value!);

        Assert.IsType<Ok>((await PatchMangaDownloadLinkEndpoint.Handle(
            context, CreateEventPublisher(), manga.MangaId, link.DownloadLinkId,
            new PatchMangaDownloadLinkEndpoint.PatchMangaDownloadLinkRequest(Matched: true, Priority: 0), ct)).Result);

        MangaDownloadLinkDto[] matchedLinks = Assert.IsType<Ok<MangaDownloadLinkDto[]>>(
            (await GetMangaDownloadLinksEndpoint.Handle(context, manga.MangaId, ct)).Result).Value!;
        Assert.Single(matchedLinks);
        Assert.Equal(link.DownloadLinkId, matchedLinks[0].DownloadId);

        // Switch the "source of truth" to candidate B - candidate A is unset, GetManga/GetMangaMetadata follow.
        Assert.IsType<Ok>((await PatchMangaMetadataEntryChosenEndpoint.Handle(context, CreateEventPublisher(), manga.MangaId, candidateB.MetadataId, ct)).Result);

        MangaDto manga2 = Assert.IsType<Ok<MangaDto>>((await GetMangaEndpoint.Handle(context, manga.MangaId, ct)).Result).Value!;
        Assert.Equal("Candidate B", manga2.MetadataEntry!.Series);

        // NOTE: GetMangaMetadataEntriesEndpoint maps DbMetadata -> Metadata via MetadataDTOHelper,
        // which never sets Chosen (that flag lives on the DbMangaMetadataEntries join row, not on
        // DbMetadata itself) - every entry here always reports Chosen == null. Which entry is
        // chosen is instead confirmed above via GetMangaEndpoint/manga2.MetadataEntry.
        MetadataDto[] relatedAfterSwitch = Assert.IsType<Ok<MetadataDto[]>>(
            (await GetMangaMetadataEntriesEndpoint.Handle(context, manga.MangaId, ct)).Result).Value!;
        Assert.Equal(2, relatedAfterSwitch.Length);
        Assert.Contains(relatedAfterSwitch, e => e.Series == "Candidate A");
        Assert.Contains(relatedAfterSwitch, e => e.Series == "Candidate B");

        // The matched Download-Link is unaffected by which Metadata-Entry is chosen.
        Assert.Single(Assert.IsType<Ok<MangaDownloadLinkDto[]>>(
            (await GetMangaDownloadLinksEndpoint.Handle(context, manga.MangaId, ct)).Result).Value!);
    }

    [Fact]
    public async Task Integration_DownloadLinkPriority_UpdatesArePersistedAndVisibleOnSubsequentReads()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga manga, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);
        DbMangaDownloadLinks first = await TestDataBuilder.SeedMangaDownloadLink(context, manga, matched: true, priority: 0, ct: ct);
        DbMangaDownloadLinks second = await TestDataBuilder.SeedMangaDownloadLink(context, manga, matched: true, priority: 1, ct: ct);

        // Promote the second link ahead of the first by giving it priority 0; the endpoint shifts
        // the conflicting entry rather than allowing two links with the same priority.
        Assert.IsType<Ok>((await PatchMangaDownloadLinkEndpoint.Handle(
            context, CreateEventPublisher(), manga.MangaId, second.DownloadLinkId,
            new PatchMangaDownloadLinkEndpoint.PatchMangaDownloadLinkRequest(Matched: true, Priority: 0), ct)).Result);

        // The shift of `first`'s Priority happened via a bulk ExecuteUpdateAsync, which bypasses
        // the change tracker. A real client would see it via a fresh request/DbContext; reopen one
        // here rather than reusing the acting context, which still holds `first`'s stale Priority.
        await using MangaContext verifyContext = MangaContextFactory.Reopen(context);
        MangaDownloadLinkDto[] links = Assert.IsType<Ok<MangaDownloadLinkDto[]>>(
            (await GetMangaDownloadLinksEndpoint.Handle(verifyContext, manga.MangaId, ct)).Result).Value!;
        Assert.Equal(0, links.Single(l => l.DownloadId == second.DownloadLinkId).Priority);
        Assert.Equal(1, links.Single(l => l.DownloadId == first.DownloadLinkId).Priority);
    }
}
