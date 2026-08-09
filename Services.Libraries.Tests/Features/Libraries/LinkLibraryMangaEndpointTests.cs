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

public sealed class LinkLibraryMangaEndpointTests : TrangaTest
{
    private static async Task<DbManga> SeedMangaWithChosenMetadata(MangaContext context, string series, CancellationToken ct)
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

        return manga;
    }

    /// <summary>
    /// Builds a full Komga "content" SeriesDto JSON array (mirrors AddKomgaEndpointTests/ChapterDownloadedHandlerTests'
    /// helper of the same shape, since every [DataMember(IsRequired = true)] field must be present).
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

    private static DbLibraryService NewKomgaLibrary(string baseUrl) =>
        new(LibraryServiceType.Komga, "MyLibrary", baseUrl, "api-key") { TrangaLibraryId = "komga-library-id" };

    [Fact]
    public async Task Handle_LinksUnmappedMangaByNameAndPushesMetadata()
    {
        int metadataUpdateCallCount = 0;
        using FakeKomgaServer server = new(path =>
        {
            if (path.Contains("/metadata"))
            {
                metadataUpdateCallCount++;
                return (HttpStatusCode.OK, null);
            }

            return (HttpStatusCode.OK, SeriesListBody(("existing-series-id", "My Manga Title")));
        });

        await using LibrariesContext context = LibrariesContextFactory.Create();
        await using MangaContext mangaContext = MangaContextFactory.Create();
        DbLibraryService library = NewKomgaLibrary(server.BaseUrl);
        await context.LibraryServices.AddAsync(library, ct);
        await context.SaveChangesAsync(ct);

        DbManga manga = await SeedMangaWithChosenMetadata(mangaContext, "My Manga Title", ct);

        Results<Ok<int>, NotFound, BadRequest<string>> result =
            await LinkLibraryMangaEndpoint.Handle(context, mangaContext, library.LibraryServiceId, NullLogger<LinkLibraryMangaEndpoint>.Instance, ct);

        Assert.Equal(1, Assert.IsType<Ok<int>>(result.Result).Value);
        Assert.Equal(1, metadataUpdateCallCount);

        DbMangaIdMapping? mapping = await context.MangaMappings
            .SingleOrDefaultAsync(m => m.LibraryServiceId == library.LibraryServiceId && m.MangaId == manga.MangaId, ct);
        Assert.NotNull(mapping);
        Assert.Equal("existing-series-id", mapping.SeriesId);
    }

    [Fact]
    public async Task Handle_SkipsAlreadyLinkedManga()
    {
        int metadataUpdateCallCount = 0;
        using FakeKomgaServer server = new(path =>
        {
            if (path.Contains("/metadata"))
            {
                metadataUpdateCallCount++;
                return (HttpStatusCode.OK, null);
            }

            return (HttpStatusCode.OK, SeriesListBody(("existing-series-id", "My Manga Title")));
        });

        await using LibrariesContext context = LibrariesContextFactory.Create();
        await using MangaContext mangaContext = MangaContextFactory.Create();
        DbLibraryService library = NewKomgaLibrary(server.BaseUrl);
        await context.LibraryServices.AddAsync(library, ct);

        DbManga manga = await SeedMangaWithChosenMetadata(mangaContext, "My Manga Title", ct);
        await context.MangaMappings.AddAsync(new DbMangaIdMapping(library.LibraryServiceId, manga.MangaId, "existing-series-id"), ct);
        await context.SaveChangesAsync(ct);

        Results<Ok<int>, NotFound, BadRequest<string>> result =
            await LinkLibraryMangaEndpoint.Handle(context, mangaContext, library.LibraryServiceId, NullLogger<LinkLibraryMangaEndpoint>.Instance, ct);

        Assert.Equal(0, Assert.IsType<Ok<int>>(result.Result).Value);
        Assert.Equal(0, metadataUpdateCallCount);
    }

    [Fact]
    public async Task Handle_UnknownLibrary_ReturnsNotFound()
    {
        await using LibrariesContext context = LibrariesContextFactory.Create();
        await using MangaContext mangaContext = MangaContextFactory.Create();

        Results<Ok<int>, NotFound, BadRequest<string>> result =
            await LinkLibraryMangaEndpoint.Handle(context, mangaContext, Guid.NewGuid(), NullLogger<LinkLibraryMangaEndpoint>.Instance, ct);

        Assert.IsType<NotFound>(result.Result);
    }
}
