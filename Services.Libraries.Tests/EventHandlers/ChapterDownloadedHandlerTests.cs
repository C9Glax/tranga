using System.Net;
using System.Reflection;
using Common.Services.Events.Events;
using Common.Tests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using Services.Libraries.Database;
using Services.Libraries.EventHandlers;
using Services.Libraries.Tests.Helpers;
using Services.Manga.Database;

namespace Services.Libraries.Tests.EventHandlers;

public sealed class ChapterDownloadedHandlerTests : TrangaTest, IDisposable
{
    private readonly int _originalMaxSeriesPollAttempts = ChapterDownloadedHandler.MaxSeriesPollAttempts;
    private readonly TimeSpan _originalSeriesPollInterval = ChapterDownloadedHandler.SeriesPollInterval;

    public void Dispose()
    {
        // HandleMessage's polling bound/interval are internal static state shared across tests
        // (kept mutable specifically so tests can shrink them); restore defaults after each test.
        ChapterDownloadedHandler.MaxSeriesPollAttempts = _originalMaxSeriesPollAttempts;
        ChapterDownloadedHandler.SeriesPollInterval = _originalSeriesPollInterval;
    }

    private static ChapterDownloadedHandler CreateHandler(LibrariesContext context, MangaContext? mangaContext = null)
    {
        Mock<IChannel> mockChannel = new();

        ServiceCollection services = new();
        services.AddSingleton(context);
        services.AddSingleton(mangaContext ?? MangaContextFactory.Create());
        services.AddLogging();
        ServiceProvider provider = services.BuildServiceProvider();

        return new ChapterDownloadedHandler(mockChannel.Object, provider);
    }

    private static async Task<(DbManga Manga, DbMetadata Metadata)> SeedMangaWithChosenMetadata(
        MangaContext context, Guid mangaId, string series, CancellationToken ct)
    {
        DbManga manga = new() { MangaId = mangaId, Monitored = true };
        DbMetadata metadata = new()
        {
            MetadataExtension = Guid.NewGuid(),
            Identifier = Guid.NewGuid().ToString(),
            Series = series,
            Summary = "Some summary"
        };
        DbMangaMetadataEntries entry = new()
        {
            MangaId = manga.MangaId,
            Chosen = true,
            Manga = manga,
            Metadata = metadata
        };

        await context.AddRangeAsync([manga, metadata, entry], ct);
        await context.SaveChangesAsync(ct);

        return (manga, metadata);
    }

    private static async Task<bool> InvokeHandleMessage(ChapterDownloadedHandler handler, ChapterDownloadedEvent chapterDownloadedEvent)
    {
        MethodInfo[] candidates = typeof(ChapterDownloadedHandler).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(m => m.Name == "HandleMessage")
            .ToArray();
        MethodInfo method = Assert.Single(candidates);
        object? result = method.Invoke(handler, [chapterDownloadedEvent]);
        Task<bool> task = Assert.IsAssignableFrom<Task<bool>>(result);
        return await task;
    }

    private static DbLibraryService NewKomgaLibrary(string baseUrl, string name = "MyLibrary") =>
        new(LibraryServiceType.Komga, name, baseUrl, "api-key")
        {
            TrangaLibraryId = "komga-library-id"
        };

    /// <summary>
    /// Builds a "content" array of full Komga SeriesDto JSON objects. Komga's generated client
    /// enforces every [DataMember(IsRequired = true)] field is present during deserialization
    /// (throws ApiException otherwise), so every required field on SeriesDto/BookMetadataAggregationDto/
    /// SeriesMetadataDto needs a value even though Komga.cs only reads Id/Name/Metadata.Summary.
    /// </summary>
    private static string SeriesListBody(params (string Id, string Name)[] series)
    {
        string items = string.Join(",", series.Select(s => $$"""
        {
            "id": "{{s.Id}}",
            "name": "{{s.Name}}",
            "libraryId": "komga-library-id",
            "booksCount": 0,
            "booksInProgressCount": 0,
            "booksReadCount": 0,
            "booksUnreadCount": 0,
            "created": "2024-01-01T00:00:00Z",
            "deleted": false,
            "fileLastModified": "2024-01-01T00:00:00Z",
            "lastModified": "2024-01-01T00:00:00Z",
            "oneshot": false,
            "url": "/some/path",
            "booksMetadata": {
                "authors": [],
                "created": "2024-01-01T00:00:00Z",
                "lastModified": "2024-01-01T00:00:00Z",
                "summary": "",
                "summaryNumber": "",
                "tags": []
            },
            "metadata": {
                "ageRatingLock": false,
                "alternateTitles": [],
                "alternateTitlesLock": false,
                "created": "2024-01-01T00:00:00Z",
                "genres": [],
                "genresLock": false,
                "language": "",
                "languageLock": false,
                "lastModified": "2024-01-01T00:00:00Z",
                "links": [],
                "linksLock": false,
                "publisher": "",
                "publisherLock": false,
                "readingDirection": "",
                "readingDirectionLock": false,
                "sharingLabels": [],
                "sharingLabelsLock": false,
                "status": "",
                "statusLock": false,
                "summary": "",
                "summaryLock": false,
                "tags": [],
                "tagsLock": false,
                "title": "",
                "titleLock": false,
                "titleSort": "",
                "titleSortLock": false,
                "totalBookCount": 0,
                "totalBookCountLock": false
            }
        }
        """));
        return $$"""{ "content": [{{items}}] }""";
    }

    private static ChapterDownloadedEvent NewEvent(Guid mangaId) =>
        new("/mangas/some-manga/chapter-1.cbz", mangaId, "Some Series", "1", "Chapter 1", null);

    [Fact]
    public async Task HandleMessage_ProcessesAllConfiguredKomgaLibraries_NotJustFirst()
    {
        // Regression test for bug 1: previously the foreach loop `return`ed after the first
        // matching Komga library, so any additional configured libraries were never processed.
        int firstLibraryScanCount = 0;
        int secondLibraryScanCount = 0;

        using FakeKomgaServer firstServer = new(path =>
        {
            if (path.Contains("/scan"))
            {
                firstLibraryScanCount++;
                return (HttpStatusCode.OK, null);
            }

            return (HttpStatusCode.OK, FakeKomgaServer.EmptySeriesListResponseBody);
        });
        using FakeKomgaServer secondServer = new(path =>
        {
            if (path.Contains("/scan"))
            {
                secondLibraryScanCount++;
                return (HttpStatusCode.OK, null);
            }

            return (HttpStatusCode.OK, FakeKomgaServer.EmptySeriesListResponseBody);
        });

        await using LibrariesContext context = LibrariesContextFactory.Create();
        DbLibraryService firstLibrary = NewKomgaLibrary(firstServer.BaseUrl, "First");
        DbLibraryService secondLibrary = NewKomgaLibrary(secondServer.BaseUrl, "Second");
        await context.LibraryServices.AddAsync(firstLibrary, ct);
        await context.LibraryServices.AddAsync(secondLibrary, ct);
        await context.SaveChangesAsync(ct);

        Guid mangaId = Guid.NewGuid();
        await context.MangaMappings.AddAsync(new DbMangaIdMapping(firstLibrary.LibraryServiceId, mangaId, "existing-series-1"), ct);
        await context.MangaMappings.AddAsync(new DbMangaIdMapping(secondLibrary.LibraryServiceId, mangaId, "existing-series-2"), ct);
        await context.SaveChangesAsync(ct);

        ChapterDownloadedHandler handler = CreateHandler(context);

        bool result = await InvokeHandleMessage(handler, NewEvent(mangaId));

        Assert.True(result);
        Assert.Equal(1, firstLibraryScanCount);
        Assert.Equal(1, secondLibraryScanCount);
    }

    [Fact]
    public async Task HandleMessage_ExistingMapping_RescansWithoutCreatingDuplicateMapping()
    {
        using FakeKomgaServer server = new(HttpStatusCode.OK, FakeKomgaServer.EmptySeriesListResponseBody);
        await using LibrariesContext context = LibrariesContextFactory.Create();
        DbLibraryService library = NewKomgaLibrary(server.BaseUrl);
        await context.LibraryServices.AddAsync(library, ct);
        await context.SaveChangesAsync(ct);

        Guid mangaId = Guid.NewGuid();
        await context.MangaMappings.AddAsync(new DbMangaIdMapping(library.LibraryServiceId, mangaId, "existing-series-id"), ct);
        await context.SaveChangesAsync(ct);

        ChapterDownloadedHandler handler = CreateHandler(context);

        bool result = await InvokeHandleMessage(handler, NewEvent(mangaId));

        Assert.True(result);
        List<DbMangaIdMapping> mappings = context.MangaMappings.Where(m => m.LibraryServiceId == library.LibraryServiceId && m.MangaId == mangaId).ToList();
        DbMangaIdMapping mapping = Assert.Single(mappings);
        Assert.Equal("existing-series-id", mapping.SeriesId);
    }

    [Fact]
    public async Task HandleMessage_NoExistingMapping_PollsUntilNewSeriesAppearsThenCreatesMapping()
    {
        ChapterDownloadedHandler.SeriesPollInterval = TimeSpan.FromMilliseconds(1);

        int seriesListCallCount = 0;
        using FakeKomgaServer server = new(path =>
        {
            if (path.Contains("/scan"))
            {
                return (HttpStatusCode.OK, null);
            }

            seriesListCallCount++;
            // First call (pre-scan) and the next poll attempt return no series; the one after
            // that simulates Komga having picked up the newly scanned series.
            return seriesListCallCount <= 2
                ? (HttpStatusCode.OK, FakeKomgaServer.EmptySeriesListResponseBody)
                : (HttpStatusCode.OK, SeriesListBody(("new-series-id", "New Series")));
        });

        await using LibrariesContext context = LibrariesContextFactory.Create();
        DbLibraryService library = NewKomgaLibrary(server.BaseUrl);
        await context.LibraryServices.AddAsync(library, ct);
        await context.SaveChangesAsync(ct);

        Guid mangaId = Guid.NewGuid();
        ChapterDownloadedHandler handler = CreateHandler(context);

        bool result = await InvokeHandleMessage(handler, NewEvent(mangaId));

        Assert.True(result);
        DbMangaIdMapping? mapping = context.MangaMappings.SingleOrDefault(m => m.LibraryServiceId == library.LibraryServiceId && m.MangaId == mangaId);
        Assert.NotNull(mapping);
        Assert.Equal("new-series-id", mapping.SeriesId);
    }

    [Fact]
    public async Task HandleMessage_NoExistingMapping_NewMappingCreated_PushesMetadataToKomga()
    {
        ChapterDownloadedHandler.SeriesPollInterval = TimeSpan.FromMilliseconds(1);

        int seriesListCallCount = 0;
        int metadataUpdateCallCount = 0;
        using FakeKomgaServer server = new(path =>
        {
            if (path.Contains("/scan"))
                return (HttpStatusCode.OK, null);

            if (path.Contains("/metadata"))
            {
                metadataUpdateCallCount++;
                return (HttpStatusCode.OK, null);
            }

            seriesListCallCount++;
            return seriesListCallCount <= 2
                ? (HttpStatusCode.OK, FakeKomgaServer.EmptySeriesListResponseBody)
                : (HttpStatusCode.OK, SeriesListBody(("new-series-id", "New Series")));
        });

        await using LibrariesContext context = LibrariesContextFactory.Create();
        await using MangaContext mangaContext = MangaContextFactory.Create();
        DbLibraryService library = NewKomgaLibrary(server.BaseUrl);
        await context.LibraryServices.AddAsync(library, ct);
        await context.SaveChangesAsync(ct);

        Guid mangaId = Guid.NewGuid();
        await SeedMangaWithChosenMetadata(mangaContext, mangaId, "New Series", ct);

        ChapterDownloadedHandler handler = CreateHandler(context, mangaContext);

        bool result = await InvokeHandleMessage(handler, NewEvent(mangaId));

        Assert.True(result);
        DbMangaIdMapping? mapping = context.MangaMappings.SingleOrDefault(m => m.LibraryServiceId == library.LibraryServiceId && m.MangaId == mangaId);
        Assert.NotNull(mapping);
        Assert.Equal(1, metadataUpdateCallCount);
    }

    [Fact]
    public async Task HandleMessage_NoExistingMapping_SeriesNeverAppears_GivesUpGracefullyWithoutCreatingMapping()
    {
        // Regression test for bug 2: previously this polled with a blocking Thread.Sleep in an
        // unbounded do/while loop, which would hang the consumer thread forever if Komga never
        // picked up the new series. Shrink the bound/interval so the test runs fast.
        ChapterDownloadedHandler.MaxSeriesPollAttempts = 3;
        ChapterDownloadedHandler.SeriesPollInterval = TimeSpan.FromMilliseconds(1);

        using FakeKomgaServer server = new(path =>
        {
            if (path.Contains("/scan"))
            {
                return (HttpStatusCode.OK, null);
            }

            // Series list never changes, no matter how many times it's polled.
            return (HttpStatusCode.OK, FakeKomgaServer.EmptySeriesListResponseBody);
        });

        await using LibrariesContext context = LibrariesContextFactory.Create();
        DbLibraryService library = NewKomgaLibrary(server.BaseUrl);
        await context.LibraryServices.AddAsync(library, ct);
        await context.SaveChangesAsync(ct);

        Guid mangaId = Guid.NewGuid();
        ChapterDownloadedHandler handler = CreateHandler(context);

        bool result = await InvokeHandleMessage(handler, NewEvent(mangaId));

        Assert.True(result);
        DbMangaIdMapping? mapping = context.MangaMappings.SingleOrDefault(m => m.LibraryServiceId == library.LibraryServiceId && m.MangaId == mangaId);
        Assert.Null(mapping);
    }

    [Fact]
    public async Task HandleMessage_NoLibrariesConfigured_ReturnsTrue()
    {
        await using LibrariesContext context = LibrariesContextFactory.Create();
        ChapterDownloadedHandler handler = CreateHandler(context);

        bool result = await InvokeHandleMessage(handler, NewEvent(Guid.NewGuid()));

        Assert.True(result);
    }

    [Fact]
    public void CanBeInstantiated()
    {
        Mock<IChannel> mockChannel = new();
        Mock<IServiceProvider> mockServiceProvider = new();

        ChapterDownloadedHandler handler = new(mockChannel.Object, mockServiceProvider.Object);

        Assert.NotNull(handler);
        Assert.IsAssignableFrom<Common.Services.Events.IEventHandler>(handler);
    }
}
