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

public sealed class AddKavitaEndpointTests : TrangaTest
{
    private const string LibrariesBody = """[{"id": 3, "name": "Manga"}]""";

    private static FakeKomgaServer KavitaServer(HttpStatusCode librariesStatus = HttpStatusCode.OK) => new(path =>
        path.EndsWith("/api/Library/libraries") ? (librariesStatus, LibrariesBody) : (HttpStatusCode.OK, "[]"));

    [Fact]
    public async Task AddKavita_PersistsLibraryBoundToKavitaLibraryId()
    {
        using FakeKomgaServer server = KavitaServer();
        await using LibrariesContext context = LibrariesContextFactory.Create();
        await using MangaContext mangaContext = MangaContextFactory.Create();
        AddKavitaEndpoint.AddKavitaLibraryRequest request = new()
        {
            Name = "MyKavita",
            BaseUrl = server.BaseUrl,
            ApiKey = "kavita-key",
            KavitaLibraryId = "3"
        };

        Results<Ok<Guid>, BadRequest<string>> result = await AddKavitaEndpoint.Handle(context, mangaContext, request, NullLogger<AddKavitaEndpoint>.Instance, ct);

        Guid id = Assert.IsType<Ok<Guid>>(result.Result).Value;
        DbLibraryService? persisted = await context.LibraryServices.FirstOrDefaultAsync(l => l.LibraryServiceId == id, ct);
        Assert.NotNull(persisted);
        Assert.Equal(LibraryServiceType.Kavita, persisted.LibraryServiceType);
        Assert.Equal("kavita-key", persisted.ApiKey);
        Assert.Equal("3", persisted.TrangaLibraryId);
    }

    [Fact]
    public async Task AddKavita_ReturnsBadRequestForUnknownLibrary()
    {
        using FakeKomgaServer server = KavitaServer();
        await using LibrariesContext context = LibrariesContextFactory.Create();
        await using MangaContext mangaContext = MangaContextFactory.Create();
        AddKavitaEndpoint.AddKavitaLibraryRequest request = new()
        {
            Name = "MyKavita",
            BaseUrl = server.BaseUrl,
            ApiKey = "kavita-key",
            KavitaLibraryId = "999"
        };

        Results<Ok<Guid>, BadRequest<string>> result = await AddKavitaEndpoint.Handle(context, mangaContext, request, NullLogger<AddKavitaEndpoint>.Instance, ct);

        Assert.IsType<BadRequest<string>>(result.Result);
        Assert.Empty(await context.LibraryServices.ToListAsync(ct));
    }

    [Fact]
    public async Task AddKavita_ReturnsBadRequestForInvalidAuthKey()
    {
        using FakeKomgaServer server = KavitaServer(HttpStatusCode.Unauthorized);
        await using LibrariesContext context = LibrariesContextFactory.Create();
        await using MangaContext mangaContext = MangaContextFactory.Create();
        AddKavitaEndpoint.AddKavitaLibraryRequest request = new()
        {
            Name = "MyKavita",
            BaseUrl = server.BaseUrl,
            ApiKey = "bad",
            KavitaLibraryId = "3"
        };

        Results<Ok<Guid>, BadRequest<string>> result = await AddKavitaEndpoint.Handle(context, mangaContext, request, NullLogger<AddKavitaEndpoint>.Instance, ct);

        Assert.IsType<BadRequest<string>>(result.Result);
        Assert.Empty(await context.LibraryServices.ToListAsync(ct));
    }
}
