using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Services.Libraries.Database;
using Services.Libraries.Features.Libraries;
using Services.Libraries.Tests.Helpers;

namespace Services.Libraries.Tests.Features.Libraries;

public sealed class GetLibraryMangaLinkEndpointTests : TrangaTest
{
    [Fact]
    public async Task GetLibraryMangaLink_ReturnsLinksWhenMappingsExist()
    {
        await using LibrariesContext context = LibrariesContextFactory.Create();
        DbLibraryService library = new(LibraryServiceType.Komga, "MyLibrary", "http://localhost:8080/", "some-api-key")
        {
            TrangaLibraryId = "tranga-lib-id"
        };
        await context.LibraryServices.AddAsync(library, ct);

        Guid mangaId = Guid.NewGuid();
        DbMangaIdMapping mapping = new(library.LibraryServiceId, mangaId, "series-42");
        await context.MangaMappings.AddAsync(mapping, ct);
        await context.SaveChangesAsync(ct);

        Ok<GetLibraryMangaLinkEndpoint.LibraryMangaLink[]> result = await GetLibraryMangaLinkEndpoint.Handle(context, mangaId, ct);

        GetLibraryMangaLinkEndpoint.LibraryMangaLink link = Assert.Single(result.Value!);
        Assert.Equal(library.LibraryServiceId, link.LibraryServiceId);
        Assert.Equal("http://localhost:8080/series/series-42", link.SeriesUrl);
    }

    [Fact]
    public async Task GetLibraryMangaLink_BuildsKavitaSeriesUrlWithLibraryId()
    {
        await using LibrariesContext context = LibrariesContextFactory.Create();
        DbLibraryService library = new(LibraryServiceType.Kavita, "MyKavita", "http://localhost:5000/", "some-api-key")
        {
            TrangaLibraryId = "3"
        };
        await context.LibraryServices.AddAsync(library, ct);

        Guid mangaId = Guid.NewGuid();
        await context.MangaMappings.AddAsync(new DbMangaIdMapping(library.LibraryServiceId, mangaId, "11"), ct);
        await context.SaveChangesAsync(ct);

        Ok<GetLibraryMangaLinkEndpoint.LibraryMangaLink[]> result = await GetLibraryMangaLinkEndpoint.Handle(context, mangaId, ct);

        Assert.Equal("http://localhost:5000/library/3/series/11", Assert.Single(result.Value!).SeriesUrl);
    }

    [Fact]
    public async Task GetLibraryMangaLink_ReturnsEmptyArrayWhenNoMappingsExist()
    {
        await using LibrariesContext context = LibrariesContextFactory.Create();

        Ok<GetLibraryMangaLinkEndpoint.LibraryMangaLink[]> result = await GetLibraryMangaLinkEndpoint.Handle(context, Guid.NewGuid(), ct);

        Assert.Empty(result.Value!);
    }
}
