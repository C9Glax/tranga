using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using Common.Services.Events.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RabbitMQ.Client;
using Services.Libraries.Database;
using Services.Libraries.EventHandlers;
using Services.Libraries.Tests.Helpers;
using Services.Manga.Database;

namespace Services.Libraries.Tests.EventHandlers;

public sealed class MangaUpdatedHandlerTests : Common.Tests.TrangaTest
{
    /// <summary>
    /// <c>HandleMessage</c> is protected, so it is invoked via reflection rather than requiring a
    /// public seam in production code (mirrors the reflection approach used elsewhere in this test
    /// project, e.g. DbLibraryToLibraryExtensionTests).
    /// </summary>
    private static async Task<bool> InvokeHandleMessage(MangaUpdatedHandler handler, MangaUpdatedEvent mangaUpdatedEvent)
    {
        MethodInfo? method = typeof(MangaUpdatedHandler).GetMethod("HandleMessage", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        object? result = method.Invoke(handler, [mangaUpdatedEvent]);
        Assert.NotNull(result);
        return await (Task<bool>)result;
    }

    /// <summary>
    /// A real DI container (rather than a mocked IServiceProvider) so the handler's
    /// <c>serviceProvider.CreateScope()</c> call resolves fresh, working LibrariesContext/MangaContext
    /// instances backed by the same underlying Sqlite databases the test seeded through the factories.
    /// </summary>
    private static ServiceProvider BuildServiceProvider(LibrariesContext librariesContext, MangaContext mangaContext)
    {
        string librariesDbPath = librariesContext.Database.GetDbConnection().DataSource;
        string mangaDbPath = mangaContext.Database.GetDbConnection().DataSource;

        ServiceCollection services = new();
        services.AddLogging();
        services.AddDbContext<LibrariesContext>(o => o.UseSqlite($"Data Source={librariesDbPath}"));
        services.AddDbContext<MangaContext>(o => o.UseSqlite($"Data Source={mangaDbPath}"));
        return services.BuildServiceProvider();
    }

    private static MangaUpdatedHandler CreateHandler(IServiceProvider serviceProvider)
    {
        Mock<IChannel> mockChannel = new();
        return new MangaUpdatedHandler(mockChannel.Object, serviceProvider);
    }

    private static async Task<(DbManga Manga, DbMetadata Metadata)> SeedMangaWithChosenMetadata(
        MangaContext context, Guid? coverId = null, CancellationToken ct = default)
    {
        DbManga manga = new() { MangaId = Guid.NewGuid(), Monitored = true };
        DbMetadata metadata = new()
        {
            MetadataExtension = Guid.NewGuid(),
            Identifier = Guid.NewGuid().ToString(),
            Series = "Updated Series Name",
            Summary = "Updated summary",
            CoverId = coverId
        };
        // MetadataId (internal set) and the entry's matching FK are populated by EF's
        // reference-navigation fixup from the Metadata navigation below, since this test project
        // does not have InternalsVisibleTo access to set DbMetadata.MetadataId directly.
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

    private static async Task<DbFile> SeedCoverFile(MangaContext context, string tempDirectory, byte[] content, CancellationToken ct)
    {
        Directory.CreateDirectory(tempDirectory);
        string fileName = $"{Guid.NewGuid():N}.jpg";
        await File.WriteAllBytesAsync(Path.Combine(tempDirectory, fileName), content, ct);

        DbFile file = new()
        {
            FileId = Guid.NewGuid(),
            Path = tempDirectory,
            Name = fileName,
            MimeType = "image/jpeg"
        };
        await context.AddAsync(file, ct);
        await context.SaveChangesAsync(ct);
        return file;
    }

    [Fact]
    public async Task HandleMessage_PushesMetadataAndPosterWhenMappingExists()
    {
        await using LibrariesContext librariesContext = LibrariesContextFactory.Create();
        await using MangaContext mangaContext = MangaContextFactory.Create();

        string tempDirectory = Path.Combine(Path.GetTempPath(), "TrangaTests", "MangaUpdatedHandlerTests", Guid.NewGuid().ToString("N"));
        DbFile cover = await SeedCoverFile(mangaContext, tempDirectory, [1, 2, 3, 4], ct);
        (DbManga manga, DbMetadata _) = await SeedMangaWithChosenMetadata(mangaContext, cover.FileId, ct);

        ConcurrentBag<string> requestedPaths = [];
        using FakeKomgaServer server = new(path =>
        {
            requestedPaths.Add(path);
            return (HttpStatusCode.OK, null);
        });

        DbLibraryService dbLibrary = new(LibraryServiceType.Komga, "MyLibrary", server.BaseUrl, "some-api-key") { TrangaLibraryId = "komga-library-id" };
        await librariesContext.AddAsync(dbLibrary, ct);
        await librariesContext.AddAsync(new DbMangaIdMapping(dbLibrary.LibraryServiceId, manga.MangaId, "series-1"), ct);
        await librariesContext.SaveChangesAsync(ct);

        await using ServiceProvider serviceProvider = BuildServiceProvider(librariesContext, mangaContext);
        MangaUpdatedHandler handler = CreateHandler(serviceProvider);

        bool result = await InvokeHandleMessage(handler, new MangaUpdatedEvent(manga.MangaId));

        Assert.True(result);
        // One request for the metadata update, one for the poster upload.
        Assert.Equal(2, requestedPaths.Count);

        Directory.Delete(tempDirectory, recursive: true);
    }

    [Fact]
    public async Task HandleMessage_NoOpWhenNoMappingExists()
    {
        await using LibrariesContext librariesContext = LibrariesContextFactory.Create();
        await using MangaContext mangaContext = MangaContextFactory.Create();

        (DbManga manga, DbMetadata _) = await SeedMangaWithChosenMetadata(mangaContext, ct: ct);

        await using ServiceProvider serviceProvider = BuildServiceProvider(librariesContext, mangaContext);
        MangaUpdatedHandler handler = CreateHandler(serviceProvider);

        bool result = await InvokeHandleMessage(handler, new MangaUpdatedEvent(manga.MangaId));

        Assert.True(result);
    }

    /// <summary>
    /// Builds a full Komga "content" SeriesDto JSON array (mirrors ChapterDownloadedHandlerTests'/
    /// LinkLibraryMangaEndpointTests' helper of the same shape, since every
    /// [DataMember(IsRequired = true)] field must be present).
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

    [Fact]
    public async Task HandleMessage_NoExistingMapping_MatchingKomgaSeriesExists_LinksAndPushesMetadataAndPoster()
    {
        // The gap this closes: choosing a metadata source for a manga ("adding" it) used to leave
        // it unlinked even if a matching Komga series already existed, until the manual /link
        // button was clicked. Now MangaUpdatedEvent (published on choosing a source) attempts the
        // same name-match linking automatically.
        await using LibrariesContext librariesContext = LibrariesContextFactory.Create();
        await using MangaContext mangaContext = MangaContextFactory.Create();

        string tempDirectory = Path.Combine(Path.GetTempPath(), "TrangaTests", "MangaUpdatedHandlerTests", Guid.NewGuid().ToString("N"));
        DbFile cover = await SeedCoverFile(mangaContext, tempDirectory, [1, 2, 3, 4], ct);
        (DbManga manga, DbMetadata _) = await SeedMangaWithChosenMetadata(mangaContext, cover.FileId, ct);

        ConcurrentBag<string> requestedPaths = [];
        using FakeKomgaServer server = new(path =>
        {
            requestedPaths.Add(path);
            if (path.Contains("/metadata") || path.Contains("/thumbnails"))
                return (HttpStatusCode.OK, null);

            // manga.Metadata.Series is "Updated Series Name" per SeedMangaWithChosenMetadata.
            return (HttpStatusCode.OK, SeriesListBody(("matched-series-id", "Updated Series Name")));
        });

        DbLibraryService dbLibrary = new(LibraryServiceType.Komga, "MyLibrary", server.BaseUrl, "some-api-key") { TrangaLibraryId = "komga-library-id" };
        await librariesContext.AddAsync(dbLibrary, ct);
        await librariesContext.SaveChangesAsync(ct);

        await using ServiceProvider serviceProvider = BuildServiceProvider(librariesContext, mangaContext);
        MangaUpdatedHandler handler = CreateHandler(serviceProvider);

        bool result = await InvokeHandleMessage(handler, new MangaUpdatedEvent(manga.MangaId));

        Assert.True(result);
        // One request for the series list (matching), one for the metadata update, one for the poster upload.
        Assert.Equal(3, requestedPaths.Count);

        DbMangaIdMapping? mapping = await librariesContext.MangaMappings
            .SingleOrDefaultAsync(m => m.LibraryServiceId == dbLibrary.LibraryServiceId && m.MangaId == manga.MangaId, ct);
        Assert.NotNull(mapping);
        Assert.Equal("matched-series-id", mapping.SeriesId);

        Directory.Delete(tempDirectory, recursive: true);
    }

    [Fact]
    public async Task HandleMessage_NoExistingMapping_NoMatchingKomgaSeries_NoMappingCreatedNoError()
    {
        await using LibrariesContext librariesContext = LibrariesContextFactory.Create();
        await using MangaContext mangaContext = MangaContextFactory.Create();

        (DbManga manga, DbMetadata _) = await SeedMangaWithChosenMetadata(mangaContext, ct: ct);

        using FakeKomgaServer server = new(HttpStatusCode.OK, SeriesListBody(("some-series-id", "Some Unrelated Series")));
        DbLibraryService dbLibrary = new(LibraryServiceType.Komga, "MyLibrary", server.BaseUrl, "some-api-key") { TrangaLibraryId = "komga-library-id" };
        await librariesContext.AddAsync(dbLibrary, ct);
        await librariesContext.SaveChangesAsync(ct);

        await using ServiceProvider serviceProvider = BuildServiceProvider(librariesContext, mangaContext);
        MangaUpdatedHandler handler = CreateHandler(serviceProvider);

        bool result = await InvokeHandleMessage(handler, new MangaUpdatedEvent(manga.MangaId));

        Assert.True(result);
        DbMangaIdMapping? mapping = await librariesContext.MangaMappings
            .SingleOrDefaultAsync(m => m.LibraryServiceId == dbLibrary.LibraryServiceId && m.MangaId == manga.MangaId, ct);
        Assert.Null(mapping);
    }

    [Fact]
    public async Task HandleMessage_SkipsMappingWhenLibraryTypeUnsupported()
    {
        await using LibrariesContext librariesContext = LibrariesContextFactory.Create();
        await using MangaContext mangaContext = MangaContextFactory.Create();

        (DbManga manga, DbMetadata _) = await SeedMangaWithChosenMetadata(mangaContext, ct: ct);

        DbLibraryService dbLibrary = new((LibraryServiceType)(-1), "MyLibrary", "http://localhost:1/", "some-api-key")
        {
            TrangaLibraryId = "komga-library-id"
        };
        await librariesContext.AddAsync(dbLibrary, ct);
        await librariesContext.AddAsync(new DbMangaIdMapping(dbLibrary.LibraryServiceId, manga.MangaId, "series-1"), ct);
        await librariesContext.SaveChangesAsync(ct);

        await using ServiceProvider serviceProvider = BuildServiceProvider(librariesContext, mangaContext);
        MangaUpdatedHandler handler = CreateHandler(serviceProvider);

        bool result = await InvokeHandleMessage(handler, new MangaUpdatedEvent(manga.MangaId));

        Assert.True(result);
    }
}
