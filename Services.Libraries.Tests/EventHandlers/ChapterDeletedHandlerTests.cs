using System.Net;
using System.Reflection;
using Common.Services.Events.Events;
using Common.Tests;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RabbitMQ.Client;
using Services.Libraries.Database;
using Services.Libraries.EventHandlers;
using Services.Libraries.Tests.Helpers;

namespace Services.Libraries.Tests.EventHandlers;

public sealed class ChapterDeletedHandlerTests : TrangaTest
{
    private static ChapterDeletedHandler CreateHandler(LibrariesContext context)
    {
        Mock<IChannel> mockChannel = new();

        ServiceCollection services = new();
        services.AddSingleton(context);
        services.AddLogging();
        ServiceProvider provider = services.BuildServiceProvider();

        return new ChapterDeletedHandler(mockChannel.Object, provider);
    }

    private static async Task<bool> InvokeHandleMessage(ChapterDeletedHandler handler, ChapterDeletedEvent chapterDeletedEvent)
    {
        MethodInfo[] candidates = typeof(ChapterDeletedHandler).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(m => m.Name == "HandleMessage")
            .ToArray();
        MethodInfo method = Assert.Single(candidates);
        object? result = method.Invoke(handler, [chapterDeletedEvent]);
        Task<bool> task = Assert.IsAssignableFrom<Task<bool>>(result);
        return await task;
    }

    private static DbLibraryService NewKomgaLibrary(string baseUrl, string name = "MyLibrary") =>
        new(LibraryServiceType.Komga, name, baseUrl, "api-key")
        {
            TrangaLibraryId = "komga-library-id"
        };

    [Fact]
    public async Task HandleMessage_LibraryWithExistingMapping_TriggersScan()
    {
        int scanCount = 0;
        using FakeKomgaServer server = new(path =>
        {
            if (path.Contains("/scan"))
            {
                scanCount++;
                return (HttpStatusCode.OK, null);
            }

            return (HttpStatusCode.OK, FakeKomgaServer.EmptySeriesListResponseBody);
        });

        await using LibrariesContext context = LibrariesContextFactory.Create();
        DbLibraryService library = NewKomgaLibrary(server.BaseUrl);
        await context.LibraryServices.AddAsync(library, ct);
        await context.SaveChangesAsync(ct);

        Guid mangaId = Guid.NewGuid();
        await context.MangaMappings.AddAsync(new DbMangaIdMapping(library.LibraryServiceId, mangaId, "existing-series-id"), ct);
        await context.SaveChangesAsync(ct);

        ChapterDeletedHandler handler = CreateHandler(context);

        bool result = await InvokeHandleMessage(handler, new ChapterDeletedEvent(mangaId));

        Assert.True(result);
        Assert.Equal(1, scanCount);
    }

    [Fact]
    public async Task HandleMessage_LibraryWithoutMappingForManga_DoesNotScan()
    {
        int scanCount = 0;
        using FakeKomgaServer server = new(path =>
        {
            if (path.Contains("/scan"))
            {
                scanCount++;
                return (HttpStatusCode.OK, null);
            }

            return (HttpStatusCode.OK, FakeKomgaServer.EmptySeriesListResponseBody);
        });

        await using LibrariesContext context = LibrariesContextFactory.Create();
        DbLibraryService library = NewKomgaLibrary(server.BaseUrl);
        await context.LibraryServices.AddAsync(library, ct);
        await context.SaveChangesAsync(ct);

        ChapterDeletedHandler handler = CreateHandler(context);

        bool result = await InvokeHandleMessage(handler, new ChapterDeletedEvent(Guid.NewGuid()));

        Assert.True(result);
        Assert.Equal(0, scanCount);
    }

    [Fact]
    public async Task HandleMessage_NoLibrariesConfigured_ReturnsTrue()
    {
        await using LibrariesContext context = LibrariesContextFactory.Create();
        ChapterDeletedHandler handler = CreateHandler(context);

        bool result = await InvokeHandleMessage(handler, new ChapterDeletedEvent(Guid.NewGuid()));

        Assert.True(result);
    }

    [Fact]
    public void CanBeInstantiated()
    {
        Mock<IChannel> mockChannel = new();
        Mock<IServiceProvider> mockServiceProvider = new();

        ChapterDeletedHandler handler = new(mockChannel.Object, mockServiceProvider.Object);

        Assert.NotNull(handler);
        Assert.IsAssignableFrom<Common.Services.Events.IEventHandler>(handler);
    }
}
