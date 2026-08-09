using System.Text;
using Common.Services.Events;
using Common.Services.Events.Events;
using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;
using RabbitMQ.Client;
using Services.Manga.Database;
using Services.Manga.Features.Manga;
using Services.Manga.Tests.Helpers;

namespace Services.Manga.Tests.Features.Manga;

public class PostMangaSyncEndpointTests : TrangaTest
{
    private static EventPublisher CreateEventPublisher(bool channelOpen = false)
    {
        Mock<IChannel> mockChannel = new();
        mockChannel.Setup(c => c.IsOpen).Returns(channelOpen);
        return new EventPublisher(mockChannel.Object);
    }

    [Fact]
    public async Task PostMangaSync_PublishesMangaUpdatedEventWithCorrectMangaId()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (DbManga manga, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);

        Mock<IChannel> mockChannel = new();
        mockChannel.Setup(c => c.IsOpen).Returns(true);
        EventPublisher eventPublisher = new(mockChannel.Object);

        Results<Ok, NotFound> result = await PostMangaSyncEndpoint.Handle(context, eventPublisher, manga.MangaId, ct);

        Assert.IsType<Ok>(result.Result);

        IInvocation publishInvocation = Assert.Single(mockChannel.Invocations, i => i.Method.Name == nameof(IChannel.BasicPublishAsync));
        Assert.Equal("tranga", publishInvocation.Arguments[0]);
        Assert.Equal(nameof(MangaUpdatedEvent), publishInvocation.Arguments[1]);
        ReadOnlyMemory<byte> body = (ReadOnlyMemory<byte>)publishInvocation.Arguments[4]!;
        Assert.Contains(manga.MangaId.ToString(), Encoding.UTF8.GetString(body.ToArray()));
    }

    [Fact]
    public async Task PostMangaSync_Returns404ForUnknownManga()
    {
        await using MangaContext context = MangaContextFactory.Create();

        Results<Ok, NotFound> result = await PostMangaSyncEndpoint.Handle(context, CreateEventPublisher(), Guid.NewGuid(), ct);

        Assert.IsType<NotFound>(result.Result);
    }
}
