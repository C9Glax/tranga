using Common.Services.Events;
using Common.Tests;
using Extensions.Custom;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Moq;
using RabbitMQ.Client;
using Services.Manga.Database;
using Services.Manga.Features.CustomDownloaders;
using Services.Manga.Tests.Helpers;

namespace Services.Manga.Tests.Features.CustomDownloaders;

/// <summary>Create, read, change and delete of custom (selector based) Download-Sources.</summary>
public class CustomDownloaderEndpointsTests : TrangaTest
{
    private static SelectorDownloaderDefinition Valid(string name = "WeebCentral") =>
        SelectorDownloaderJson.WeebCentralExample() with { Id = Guid.Empty, Name = name };

    private static (EventPublisher Publisher, Mock<IChannel> Channel) CreateEventPublisher(bool channelOpen = true)
    {
        Mock<IChannel> channel = new();
        channel.Setup(c => c.IsOpen).Returns(channelOpen);
        return (new EventPublisher(channel.Object), channel);
    }

    private static bool PublishedChangedEvent(Mock<IChannel> channel) =>
        channel.Invocations.Any(i => i.Method.Name == "BasicPublishAsync" && i.Arguments.Contains("DownloadExtensionsChangedEvent"));

    #region Post

    [Fact]
    public async Task Post_StoresTheSourceUnderANewIdAndPublishesAnEvent()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (EventPublisher publisher, Mock<IChannel> channel) = CreateEventPublisher();
        Guid suppliedId = Guid.NewGuid();

        Results<Ok<SelectorDownloaderDefinition>, BadRequest<string[]>> result =
            await PostCustomDownloaderEndpoint.Handle(context, publisher, Valid() with { Id = suppliedId }, ct);

        Ok<SelectorDownloaderDefinition> ok = Assert.IsType<Ok<SelectorDownloaderDefinition>>(result.Result);
        Assert.NotEqual(Guid.Empty, ok.Value!.Id);
        Assert.NotEqual(suppliedId, ok.Value.Id);
        DbCustomDownloader row = await context.CustomDownloaders.SingleAsync(ct);
        Assert.Equal(ok.Value.Id, row.CustomDownloaderId);
        Assert.Equal("WeebCentral", row.Name);
        Assert.Equal(ok.Value.Search, SelectorDownloaderJson.Deserialize(row.DefinitionJson)!.Search);
        Assert.True(PublishedChangedEvent(channel));
    }

    [Fact]
    public async Task Post_StoresTheBaseUrlWithoutTrailingSlash()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (EventPublisher publisher, _) = CreateEventPublisher();

        Results<Ok<SelectorDownloaderDefinition>, BadRequest<string[]>> result =
            await PostCustomDownloaderEndpoint.Handle(context, publisher, Valid() with { BaseUrl = "https://weebcentral.com/ " }, ct);

        Assert.Equal("https://weebcentral.com", Assert.IsType<Ok<SelectorDownloaderDefinition>>(result.Result).Value!.BaseUrl);
        Assert.Equal("https://weebcentral.com", SelectorDownloaderJson.Deserialize((await context.CustomDownloaders.SingleAsync(ct)).DefinitionJson)!.BaseUrl);
    }

    [Fact]
    public async Task Post_StillSucceedsWhenTheMessageBusIsDown()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (EventPublisher publisher, _) = CreateEventPublisher(channelOpen: false);

        Results<Ok<SelectorDownloaderDefinition>, BadRequest<string[]>> result =
            await PostCustomDownloaderEndpoint.Handle(context, publisher, Valid(), ct);

        Assert.IsType<Ok<SelectorDownloaderDefinition>>(result.Result);
        Assert.Single(await context.CustomDownloaders.ToListAsync(ct));
    }

    [Fact]
    public async Task Post_RejectsInvalidDefinitionsAndListsAllProblems()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (EventPublisher publisher, Mock<IChannel> channel) = CreateEventPublisher();

        Results<Ok<SelectorDownloaderDefinition>, BadRequest<string[]>> result = await PostCustomDownloaderEndpoint.Handle(
            context, publisher, Valid() with { Name = "", BaseUrl = "nonsense", Search = new SearchSpec() }, ct);

        BadRequest<string[]> badRequest = Assert.IsType<BadRequest<string[]>>(result.Result);
        Assert.Contains(badRequest.Value!, e => e.Contains("Name"));
        Assert.Contains(badRequest.Value!, e => e.Contains("Base URL"));
        Assert.Contains(badRequest.Value!, e => e.Contains("Search"));
        Assert.Empty(await context.CustomDownloaders.ToListAsync(ct));
        Assert.False(PublishedChangedEvent(channel));
    }

    #endregion

    #region Get

    [Fact]
    public async Task GetList_ReturnsSourcesSortedByNameAndSkipsUnreadableOnes()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (EventPublisher publisher, _) = CreateEventPublisher();
        await PostCustomDownloaderEndpoint.Handle(context, publisher, Valid("Zeta"), ct);
        await PostCustomDownloaderEndpoint.Handle(context, publisher, Valid("Alpha"), ct);
        await context.CustomDownloaders.AddAsync(new DbCustomDownloader { CustomDownloaderId = Guid.NewGuid(), Name = "Broken", DefinitionJson = "not json" }, ct);
        await context.SaveChangesAsync(ct);

        Ok<List<SelectorDownloaderDefinition>> result = await GetCustomDownloadersEndpoint.Handle(context, ct);

        Assert.Equal(["Alpha", "Zeta"], result.Value!.Select(d => d.Name));
    }

    [Fact]
    public async Task Get_ReturnsTheDefinitionWithTheRowsId()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (EventPublisher publisher, _) = CreateEventPublisher();
        Guid id = (await PostCustomDownloaderEndpoint.Handle(context, publisher, Valid(), ct)).Result is Ok<SelectorDownloaderDefinition> created ? created.Value!.Id : Guid.Empty;

        Results<Ok<SelectorDownloaderDefinition>, NotFound> result = await GetCustomDownloaderEndpoint.Handle(context, id, ct);

        Assert.Equal(id, Assert.IsType<Ok<SelectorDownloaderDefinition>>(result.Result).Value!.Id);
    }

    [Fact]
    public async Task Get_Returns404ForUnknownId()
    {
        await using MangaContext context = MangaContextFactory.Create();

        Results<Ok<SelectorDownloaderDefinition>, NotFound> result = await GetCustomDownloaderEndpoint.Handle(context, Guid.NewGuid(), ct);

        Assert.IsType<NotFound>(result.Result);
    }

    #endregion

    #region Put

    [Fact]
    public async Task Put_ChangesTheSourceAndKeepsItsId()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (EventPublisher publisher, Mock<IChannel> channel) = CreateEventPublisher();
        Guid id = ((Ok<SelectorDownloaderDefinition>)(await PostCustomDownloaderEndpoint.Handle(context, publisher, Valid("Old"), ct)).Result).Value!.Id;
        channel.Invocations.Clear();

        Results<Ok<SelectorDownloaderDefinition>, BadRequest<string[]>, NotFound> result = await PutCustomDownloaderEndpoint.Handle(
            context, publisher, id, Valid("New") with { Id = Guid.NewGuid(), RequestsPerSecond = 2 }, ct);

        Ok<SelectorDownloaderDefinition> ok = Assert.IsType<Ok<SelectorDownloaderDefinition>>(result.Result);
        Assert.Equal(id, ok.Value!.Id);
        DbCustomDownloader row = await context.CustomDownloaders.AsNoTracking().SingleAsync(ct);
        Assert.Equal(id, row.CustomDownloaderId);
        Assert.Equal("New", row.Name);
        Assert.Equal(2, SelectorDownloaderJson.Deserialize(row.DefinitionJson)!.RequestsPerSecond);
        Assert.True(PublishedChangedEvent(channel));
    }

    [Fact]
    public async Task Put_RejectsInvalidDefinitionsWithoutChangingAnything()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (EventPublisher publisher, _) = CreateEventPublisher();
        Guid id = ((Ok<SelectorDownloaderDefinition>)(await PostCustomDownloaderEndpoint.Handle(context, publisher, Valid("Keep"), ct)).Result).Value!.Id;

        Results<Ok<SelectorDownloaderDefinition>, BadRequest<string[]>, NotFound> result =
            await PutCustomDownloaderEndpoint.Handle(context, publisher, id, Valid("Changed") with { RequestsPerSecond = 500 }, ct);

        Assert.IsType<BadRequest<string[]>>(result.Result);
        Assert.Equal("Keep", (await context.CustomDownloaders.AsNoTracking().SingleAsync(ct)).Name);
    }

    [Fact]
    public async Task Put_Returns404ForUnknownId()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (EventPublisher publisher, _) = CreateEventPublisher();

        Results<Ok<SelectorDownloaderDefinition>, BadRequest<string[]>, NotFound> result =
            await PutCustomDownloaderEndpoint.Handle(context, publisher, Guid.NewGuid(), Valid(), ct);

        Assert.IsType<NotFound>(result.Result);
    }

    #endregion

    #region Delete

    [Fact]
    public async Task Delete_RemovesTheSourceAndPublishesAnEvent()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (EventPublisher publisher, Mock<IChannel> channel) = CreateEventPublisher();
        Guid id = ((Ok<SelectorDownloaderDefinition>)(await PostCustomDownloaderEndpoint.Handle(context, publisher, Valid(), ct)).Result).Value!.Id;
        channel.Invocations.Clear();

        Results<Ok, NotFound> result = await DeleteCustomDownloaderEndpoint.Handle(context, publisher, id, ct);

        Assert.IsType<Ok>(result.Result);
        Assert.Empty(await context.CustomDownloaders.ToListAsync(ct));
        Assert.True(PublishedChangedEvent(channel));
    }

    [Fact]
    public async Task Delete_KeepsDownloadLinksThatUsedTheSource()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (EventPublisher publisher, _) = CreateEventPublisher();
        Guid id = ((Ok<SelectorDownloaderDefinition>)(await PostCustomDownloaderEndpoint.Handle(context, publisher, Valid(), ct)).Result).Value!.Id;
        (DbManga manga, _, _) = await TestDataBuilder.SeedMangaWithChosenMetadata(context, ct: ct);
        DbDownloadLink downloadLink = new() { DownloadExtension = id, Identifier = "https://example.com/series/1", Series = "Series" };
        DbMangaDownloadLinks link = new()
        {
            MangaId = manga.MangaId, DownloadLinkId = downloadLink.DownloadLinkId, Matched = true, Priority = 0, Manga = manga, DownloadLink = downloadLink
        };
        await context.AddAsync(downloadLink, ct);
        await context.AddAsync(link, ct);
        await context.SaveChangesAsync(ct);

        await DeleteCustomDownloaderEndpoint.Handle(context, publisher, id, ct);

        Assert.Single(await context.MangaDownloadLinks.AsNoTracking().Where(l => l.DownloadLinkId == link.DownloadLinkId).ToListAsync(ct));
    }

    [Fact]
    public async Task Delete_Returns404ForUnknownId()
    {
        await using MangaContext context = MangaContextFactory.Create();
        (EventPublisher publisher, _) = CreateEventPublisher();

        Results<Ok, NotFound> result = await DeleteCustomDownloaderEndpoint.Handle(context, publisher, Guid.NewGuid(), ct);

        Assert.IsType<NotFound>(result.Result);
    }

    #endregion
}
