using System.Text;
using Common.Services.Events;
using Common.Services.Events.Events;
using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Moq;
using RabbitMQ.Client;
using Services.Manga.Database;
using Services.Manga.Features.Manga;
using Services.Manga.Tests.Helpers;

namespace Services.Manga.Tests.Features.Manga;

public class PatchMangaMetadataEntryChosenEndpointTests : TrangaTest
{
    private static EventPublisher CreateEventPublisher(bool channelOpen = false)
    {
        Mock<IChannel> mockChannel = new();
        mockChannel.Setup(c => c.IsOpen).Returns(channelOpen);
        return new EventPublisher(mockChannel.Object);
    }

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

        Results<Ok, NotFound> result = await PatchMangaMetadataEntryChosenEndpoint.Handle(context, CreateEventPublisher(channelOpen: true), manga.MangaId, toChoose.MetadataId, ct);

        Assert.IsType<Ok>(result.Result);

        // AsNoTracking: the endpoint changes `previousEntry`'s Chosen flag via a bulk
        // ExecuteUpdateAsync, which bypasses the change tracker - a tracking query here would
        // return the stale in-memory value seeded above instead of what was actually persisted.
        List<DbMangaMetadataEntries> entries = await context.MangaMetadataEntries.AsNoTracking().Where(e => e.MangaId == manga.MangaId).ToListAsync(ct);
        Assert.True(entries.Single(e => e.MetadataId == toChoose.MetadataId).Chosen);
        Assert.False(entries.Single(e => e.MetadataId == previouslyChosen.MetadataId).Chosen);
    }

    [Fact]
    public async Task PatchMangaMetadata_PublishesMangaUpdatedEventWithCorrectMangaId()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga manga, DbMetadata metadata, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);

        Mock<IChannel> mockChannel = new();
        mockChannel.Setup(c => c.IsOpen).Returns(true);
        EventPublisher eventPublisher = new(mockChannel.Object);

        Results<Ok, NotFound> result = await PatchMangaMetadataEntryChosenEndpoint.Handle(context, eventPublisher, manga.MangaId, metadata.MetadataId, ct);

        Assert.IsType<Ok>(result.Result);

        // BasicPublishAsync<TProperties> is a constrained generic method (TProperties : IReadOnlyBasicProperties)
        // closed over an internal RabbitMQ.Client type at the call site, so it can't be targeted by a
        // strongly-typed Setup/Verify expression. Inspect the recorded invocation instead.
        IInvocation publishInvocation = Assert.Single(mockChannel.Invocations, i => i.Method.Name == nameof(IChannel.BasicPublishAsync));
        Assert.Equal("tranga", publishInvocation.Arguments[0]);
        Assert.Equal(nameof(MangaUpdatedEvent), publishInvocation.Arguments[1]);
        ReadOnlyMemory<byte> body = (ReadOnlyMemory<byte>)publishInvocation.Arguments[4]!;
        Assert.Contains(manga.MangaId.ToString(), Encoding.UTF8.GetString(body.ToArray()));
    }

    [Fact]
    public async Task PatchMangaMetadata_Returns404ForUnknownManga()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (_, DbMetadata metadata, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);

        Results<Ok, NotFound> result = await PatchMangaMetadataEntryChosenEndpoint.Handle(context, CreateEventPublisher(), Guid.NewGuid(), metadata.MetadataId, ct);

        Assert.IsType<NotFound>(result.Result);
    }

    [Fact]
    public async Task PatchMangaMetadata_Returns404ForUnknownMetadataId()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga manga, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);

        Results<Ok, NotFound> result = await PatchMangaMetadataEntryChosenEndpoint.Handle(context, CreateEventPublisher(), manga.MangaId, Guid.NewGuid(), ct);

        Assert.IsType<NotFound>(result.Result);
    }
}
