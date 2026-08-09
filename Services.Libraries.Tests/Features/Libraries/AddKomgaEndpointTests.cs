using System.Net;
using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Services.Libraries.Database;
using Services.Libraries.Features.Libraries;
using Services.Libraries.Tests.Helpers;
using Services.Manga.Database;

namespace Services.Libraries.Tests.Features.Libraries;

public sealed class AddKomgaEndpointTests : TrangaTest
{
    private static async Task<(DbManga Manga, DbMetadata Metadata)> SeedMangaWithChosenMetadata(
        MangaContext context, string series, CancellationToken ct)
    {
        DbManga manga = new() { MangaId = Guid.NewGuid(), Monitored = true };
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

    [Fact]
    public async Task AddKomga_CreatesLibraryWithApiKey()
    {
        using FakeKomgaServer server = new(path => path.Contains("/libraries")
            ? (HttpStatusCode.OK, FakeKomgaServer.ValidLibraryCreationResponseBody)
            : (HttpStatusCode.OK, FakeKomgaServer.EmptySeriesListResponseBody));
        await using LibrariesContext context = LibrariesContextFactory.Create();
        await using MangaContext mangaContext = MangaContextFactory.Create();

        AddKomgaEndpoint.AddKomgaLibraryRequest request = new()
        {
            Name = "MyLibrary",
            BaseUrl = server.BaseUrl,
            ApiKey = "some-api-key"
        };

        Results<Ok<Guid>, BadRequest<string>> result = await AddKomgaEndpoint.Handle(context, mangaContext, request, NullLogger<AddKomgaEndpoint>.Instance, ct);

        Guid id = Assert.IsType<Ok<Guid>>(result.Result).Value;
        DbLibraryService? persisted = await context.LibraryServices.FirstOrDefaultAsync(l => l.LibraryServiceId == id, ct);
        Assert.NotNull(persisted);
        Assert.Equal("some-api-key", persisted.ApiKey);
        Assert.Null(persisted.Username);
    }

    [Fact]
    public async Task AddKomga_CreatesLibraryWithUsernameAndPassword()
    {
        using FakeKomgaServer server = new(request => request.Contains("api-keys")
            ? (HttpStatusCode.OK, FakeKomgaServer.ValidApiKeyMintResponseBody)
            : request.Contains("/libraries")
                ? (HttpStatusCode.OK, FakeKomgaServer.ValidLibraryCreationResponseBody)
                : (HttpStatusCode.OK, FakeKomgaServer.EmptySeriesListResponseBody));
        await using LibrariesContext context = LibrariesContextFactory.Create();
        await using MangaContext mangaContext = MangaContextFactory.Create();

        AddKomgaEndpoint.AddKomgaLibraryRequest request = new()
        {
            Name = "MyLibrary",
            BaseUrl = server.BaseUrl,
            Username = "someuser",
            Password = "somepassword"
        };

        Results<Ok<Guid>, BadRequest<string>> result = await AddKomgaEndpoint.Handle(context, mangaContext, request, NullLogger<AddKomgaEndpoint>.Instance, ct);

        Guid id = Assert.IsType<Ok<Guid>>(result.Result).Value;
        DbLibraryService? persisted = await context.LibraryServices.FirstOrDefaultAsync(l => l.LibraryServiceId == id, ct);
        Assert.NotNull(persisted);
        Assert.Equal("minted-api-key-value", persisted.ApiKey);
        Assert.Equal("someuser", persisted.Username);
    }

    [Fact]
    public async Task AddKomga_RejectsInvalidCredentials()
    {
        using FakeKomgaServer server = new(HttpStatusCode.Unauthorized);
        await using LibrariesContext context = LibrariesContextFactory.Create();
        await using MangaContext mangaContext = MangaContextFactory.Create();

        AddKomgaEndpoint.AddKomgaLibraryRequest request = new()
        {
            Name = "MyLibrary",
            BaseUrl = server.BaseUrl,
            Username = "someuser",
            Password = "wrongpassword"
        };

        Results<Ok<Guid>, BadRequest<string>> result = await AddKomgaEndpoint.Handle(context, mangaContext, request, NullLogger<AddKomgaEndpoint>.Instance, ct);

        Assert.IsType<BadRequest<string>>(result.Result);
        Assert.Empty(await context.LibraryServices.ToListAsync(ct));
    }

    [Fact]
    public async Task AddKomga_RejectsWhenNeitherAuthModeGiven()
    {
        await using LibrariesContext context = LibrariesContextFactory.Create();
        await using MangaContext mangaContext = MangaContextFactory.Create();

        AddKomgaEndpoint.AddKomgaLibraryRequest request = new()
        {
            Name = "MyLibrary",
            BaseUrl = "http://localhost:8080"
        };

        Results<Ok<Guid>, BadRequest<string>> result = await AddKomgaEndpoint.Handle(context, mangaContext, request, NullLogger<AddKomgaEndpoint>.Instance, ct);

        Assert.IsType<BadRequest<string>>(result.Result);
        Assert.Empty(await context.LibraryServices.ToListAsync(ct));
    }

    [Fact]
    public async Task AddKomga_RejectsWhenBothAuthModesGiven()
    {
        await using LibrariesContext context = LibrariesContextFactory.Create();
        await using MangaContext mangaContext = MangaContextFactory.Create();

        AddKomgaEndpoint.AddKomgaLibraryRequest request = new()
        {
            Name = "MyLibrary",
            BaseUrl = "http://localhost:8080",
            ApiKey = "some-api-key",
            Username = "someuser",
            Password = "somepassword"
        };

        Results<Ok<Guid>, BadRequest<string>> result = await AddKomgaEndpoint.Handle(context, mangaContext, request, NullLogger<AddKomgaEndpoint>.Instance, ct);

        Assert.IsType<BadRequest<string>>(result.Result);
        Assert.Empty(await context.LibraryServices.ToListAsync(ct));
    }

    [Fact]
    public async Task AddKomga_LinksExistingMangaByNameAndPushesMetadata()
    {
        int metadataUpdateCallCount = 0;
        using FakeKomgaServer server = new(path =>
        {
            if (path.Contains("/libraries"))
                return (HttpStatusCode.OK, FakeKomgaServer.ValidLibraryCreationResponseBody);
            if (path.Contains("/metadata"))
            {
                metadataUpdateCallCount++;
                return (HttpStatusCode.OK, null);
            }

            // GetSeriesList
            return (HttpStatusCode.OK, ChapterDownloadedHandlerTestsSeriesListBody(("existing-series-id", "My Manga Title")));
        });
        await using LibrariesContext context = LibrariesContextFactory.Create();
        await using MangaContext mangaContext = MangaContextFactory.Create();
        (DbManga manga, DbMetadata _) = await SeedMangaWithChosenMetadata(mangaContext, "My Manga Title", ct);

        AddKomgaEndpoint.AddKomgaLibraryRequest request = new()
        {
            Name = "MyLibrary",
            BaseUrl = server.BaseUrl,
            ApiKey = "some-api-key"
        };

        Results<Ok<Guid>, BadRequest<string>> result = await AddKomgaEndpoint.Handle(context, mangaContext, request, NullLogger<AddKomgaEndpoint>.Instance, ct);

        Guid id = Assert.IsType<Ok<Guid>>(result.Result).Value;
        DbMangaIdMapping? mapping = await context.MangaMappings
            .SingleOrDefaultAsync(m => m.LibraryServiceId == id && m.MangaId == manga.MangaId, ct);
        Assert.NotNull(mapping);
        Assert.Equal("existing-series-id", mapping.SeriesId);
        Assert.Equal(1, metadataUpdateCallCount);
    }

    [Fact]
    public async Task AddKomga_NoMatchingSeriesName_LeavesMangaUnlinked()
    {
        using FakeKomgaServer server = new(path => path.Contains("/libraries")
            ? (HttpStatusCode.OK, FakeKomgaServer.ValidLibraryCreationResponseBody)
            : (HttpStatusCode.OK, ChapterDownloadedHandlerTestsSeriesListBody(("existing-series-id", "Some Other Series"))));
        await using LibrariesContext context = LibrariesContextFactory.Create();
        await using MangaContext mangaContext = MangaContextFactory.Create();
        (DbManga manga, DbMetadata _) = await SeedMangaWithChosenMetadata(mangaContext, "My Manga Title", ct);

        AddKomgaEndpoint.AddKomgaLibraryRequest request = new()
        {
            Name = "MyLibrary",
            BaseUrl = server.BaseUrl,
            ApiKey = "some-api-key"
        };

        Results<Ok<Guid>, BadRequest<string>> result = await AddKomgaEndpoint.Handle(context, mangaContext, request, NullLogger<AddKomgaEndpoint>.Instance, ct);

        Guid id = Assert.IsType<Ok<Guid>>(result.Result).Value;
        Assert.Empty(await context.MangaMappings.Where(m => m.LibraryServiceId == id && m.MangaId == manga.MangaId).ToListAsync(ct));
    }

    /// <summary>
    /// Builds a full Komga "content" SeriesDto JSON array, matching the shape required by
    /// <see cref="ChapterDownloadedHandlerTests"/>'s SeriesListBody helper (every
    /// [DataMember(IsRequired = true)] field must be present or the generated client throws).
    /// </summary>
    private static string ChapterDownloadedHandlerTestsSeriesListBody(params (string Id, string Name)[] series)
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
}
