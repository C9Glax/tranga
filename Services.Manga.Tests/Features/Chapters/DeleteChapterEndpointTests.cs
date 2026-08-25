using System.Text;
using Common.Services.Events;
using Common.Services.Events.Events;
using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Moq;
using RabbitMQ.Client;
using Services.Manga.Database;
using Services.Manga.Features.Chapters;
using Services.Manga.Tests.Helpers;

namespace Services.Manga.Tests.Features.Chapters;

public class DeleteChapterEndpointTests : TrangaTest
{
    private static EventPublisher CreateEventPublisher(bool channelOpen = false)
    {
        Mock<IChannel> mockChannel = new();
        mockChannel.Setup(c => c.IsOpen).Returns(channelOpen);
        return new EventPublisher(mockChannel.Object);
    }

    [Fact]
    public async Task DeleteChapter_RemovesChapterAndDownloadedFile()
    {
        await using MangaContext context = MangaContextFactory.Create();
        DbManga manga = TestDataBuilder.NewManga();
        await context.AddAsync(manga, ct);
        await context.SaveChangesAsync(ct);
        DbChapter chapter = await TestDataBuilder.SeedChapter(context, manga, ct: ct);
        DbChapterDownloadLink link = await TestDataBuilder.SeedChapterDownloadLink(context, chapter, downloaded: true, ct: ct);
        Guid fileId = link.FileId!.Value;

        Results<Ok, NotFound> result = await DeleteChapterEndpoint.Handle(context, CreateEventPublisher(channelOpen: true), chapter.ChapterId, ct);

        Assert.IsType<Ok>(result.Result);
        Assert.False(await context.Chapters.AnyAsync(c => c.ChapterId == chapter.ChapterId, ct));
        Assert.False(await context.ChapterDownloadLinks.AnyAsync(l => l.ChapterId == chapter.ChapterId, ct));
        Assert.False(await context.Files.AnyAsync(f => f.FileId == fileId, ct));
    }

    [Fact]
    public async Task DeleteChapter_PublishesChapterDeletedEventWithMangaId()
    {
        await using MangaContext context = MangaContextFactory.Create();
        DbManga manga = TestDataBuilder.NewManga();
        await context.AddAsync(manga, ct);
        await context.SaveChangesAsync(ct);
        DbChapter chapter = await TestDataBuilder.SeedChapter(context, manga, ct: ct);

        Mock<IChannel> mockChannel = new();
        mockChannel.Setup(c => c.IsOpen).Returns(true);
        EventPublisher eventPublisher = new(mockChannel.Object);

        Results<Ok, NotFound> result = await DeleteChapterEndpoint.Handle(context, eventPublisher, chapter.ChapterId, ct);

        Assert.IsType<Ok>(result.Result);
        IInvocation publishInvocation = Assert.Single(mockChannel.Invocations, i => i.Method.Name == nameof(IChannel.BasicPublishAsync));
        Assert.Equal(nameof(ChapterDeletedEvent), publishInvocation.Arguments[1]);
        ReadOnlyMemory<byte> body = (ReadOnlyMemory<byte>)publishInvocation.Arguments[4]!;
        Assert.Contains(manga.MangaId.ToString(), Encoding.UTF8.GetString(body.ToArray()));
    }

    [Fact]
    public async Task DeleteChapter_Returns404ForUnknownId()
    {
        await using MangaContext context = MangaContextFactory.Create();

        Results<Ok, NotFound> result = await DeleteChapterEndpoint.Handle(context, CreateEventPublisher(), Guid.NewGuid(), ct);

        Assert.IsType<NotFound>(result.Result);
    }
}
