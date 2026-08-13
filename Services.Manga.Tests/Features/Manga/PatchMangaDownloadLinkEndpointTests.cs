using Common.Services.Events;
using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Moq;
using RabbitMQ.Client;
using Services.Manga.Database;
using Services.Manga.Features.Manga;
using Services.Manga.Tests.Helpers;

namespace Services.Manga.Tests.Features.Manga;

public class PatchMangaDownloadLinkEndpointTests : TrangaTest
{
    private static EventPublisher CreateEventPublisher(bool channelOpen = false)
    {
        Mock<IChannel> mockChannel = new();
        mockChannel.Setup(c => c.IsOpen).Returns(channelOpen);
        return new EventPublisher(mockChannel.Object);
    }

    [Fact]
    public async Task PatchMangaDownloadLink_SetsPriority()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga manga, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);
        DbMangaDownloadLinks link = await TestDataBuilder.SeedMangaDownloadLink(context, manga, matched: false, priority: 0, ct: ct);

        Results<Ok, NotFound> result = await PatchMangaDownloadLinkEndpoint.Handle(
            context, CreateEventPublisher(), manga.MangaId, link.DownloadLinkId,
            new PatchMangaDownloadLinkEndpoint.PatchMangaDownloadLinkRequest(Matched: false, Priority: 5), ct);

        Assert.IsType<Ok>(result.Result);
        DbMangaDownloadLinks updated = await context.MangaDownloadLinks.SingleAsync(l => l.DownloadLinkId == link.DownloadLinkId, ct);
        Assert.Equal(5, updated.Priority);
    }

    [Fact]
    public async Task PatchMangaDownloadLink_ShiftsExistingPriorityWhenAlreadyTaken()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga manga, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);
        DbMangaDownloadLinks first = await TestDataBuilder.SeedMangaDownloadLink(context, manga, matched: false, priority: 0, ct: ct);
        DbMangaDownloadLinks second = await TestDataBuilder.SeedMangaDownloadLink(context, manga, matched: false, priority: 1, ct: ct);

        // second already holds priority 1; move first onto priority 1 too, expect second to shift to 2
        Results<Ok, NotFound> result = await PatchMangaDownloadLinkEndpoint.Handle(
            context, CreateEventPublisher(), manga.MangaId, first.DownloadLinkId,
            new PatchMangaDownloadLinkEndpoint.PatchMangaDownloadLinkRequest(Matched: false, Priority: 1), ct);

        Assert.IsType<Ok>(result.Result);
        // AsNoTracking: `second`'s Priority shift happens via a bulk ExecuteUpdateAsync, which
        // bypasses the change tracker - a tracking query would return the stale seeded value.
        Assert.Equal(1, (await context.MangaDownloadLinks.AsNoTracking().SingleAsync(l => l.DownloadLinkId == first.DownloadLinkId, ct)).Priority);
        Assert.Equal(2, (await context.MangaDownloadLinks.AsNoTracking().SingleAsync(l => l.DownloadLinkId == second.DownloadLinkId, ct)).Priority);
    }

    [Fact]
    public async Task PatchMangaDownloadLink_PublishesEventWhenMatched()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga manga, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);
        DbMangaDownloadLinks link = await TestDataBuilder.SeedMangaDownloadLink(context, manga, matched: false, priority: 0, ct: ct);

        Results<Ok, NotFound> result = await PatchMangaDownloadLinkEndpoint.Handle(
            context, CreateEventPublisher(channelOpen: true), manga.MangaId, link.DownloadLinkId,
            new PatchMangaDownloadLinkEndpoint.PatchMangaDownloadLinkRequest(Matched: true, Priority: 0), ct);

        Assert.IsType<Ok>(result.Result);
        Assert.True((await context.MangaDownloadLinks.SingleAsync(l => l.DownloadLinkId == link.DownloadLinkId, ct)).Matched);
    }

    [Fact]
    public async Task PatchMangaDownloadLink_SetsMangaMonitoredWhenMatched()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga manga, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, monitored: false, ct: ct);
        DbMangaDownloadLinks link = await TestDataBuilder.SeedMangaDownloadLink(context, manga, matched: false, priority: 0, ct: ct);
        Assert.False(manga.Monitored);

        Results<Ok, NotFound> result = await PatchMangaDownloadLinkEndpoint.Handle(
            context, CreateEventPublisher(channelOpen: true), manga.MangaId, link.DownloadLinkId,
            new PatchMangaDownloadLinkEndpoint.PatchMangaDownloadLinkRequest(Matched: true, Priority: 0), ct);

        Assert.IsType<Ok>(result.Result);
        Assert.True((await context.Mangas.SingleAsync(m => m.MangaId == manga.MangaId, ct)).Monitored);
    }

    [Fact]
    public async Task PatchMangaDownloadLink_Returns404ForUnknownLink()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga manga, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);

        Results<Ok, NotFound> result = await PatchMangaDownloadLinkEndpoint.Handle(
            context, CreateEventPublisher(), manga.MangaId, Guid.NewGuid(),
            new PatchMangaDownloadLinkEndpoint.PatchMangaDownloadLinkRequest(Matched: false, Priority: 0), ct);

        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task PatchMangaDownloadLink_Returns404ForUnknownManga()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga manga, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);
        DbMangaDownloadLinks link = await TestDataBuilder.SeedMangaDownloadLink(context, manga, ct: ct);

        Results<Ok, NotFound> result = await PatchMangaDownloadLinkEndpoint.Handle(
            context, CreateEventPublisher(), Guid.NewGuid(), link.DownloadLinkId,
            new PatchMangaDownloadLinkEndpoint.PatchMangaDownloadLinkRequest(Matched: false, Priority: 0), ct);

        Assert.IsType<NotFound>(result.Result);
    }
}
