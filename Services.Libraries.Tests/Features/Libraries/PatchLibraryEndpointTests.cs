using System.Net;
using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Services.Libraries.Database;
using Services.Libraries.Features.Libraries;
using Services.Libraries.Tests.Helpers;

namespace Services.Libraries.Tests.Features.Libraries;

public sealed class PatchLibraryEndpointTests : TrangaTest
{
    private static async Task<DbLibraryService> SeedLibrary(LibrariesContext context, string baseUrl, CancellationToken ct)
    {
        DbLibraryService library = new(LibraryServiceType.Komga, "MyLibrary", baseUrl, "original-api-key")
        {
            TrangaLibraryId = "tranga-lib-id"
        };
        await context.LibraryServices.AddAsync(library, ct);
        await context.SaveChangesAsync(ct);
        return library;
    }

    [Fact]
    public async Task PatchLibrary_RenamesLibrary()
    {
        await using LibrariesContext context = LibrariesContextFactory.Create();
        DbLibraryService library = await SeedLibrary(context, "http://localhost:8080", ct);

        PatchLibraryEndpoint.PatchLibraryRequest request = new() { Name = "RenamedLibrary" };

        Results<Ok, BadRequest<string>, NotFound> result = await PatchLibraryEndpoint.Handle(context, library.LibraryServiceId, request, ct);

        Assert.IsType<Ok>(result.Result);
        DbLibraryService? updated = await context.LibraryServices.AsNoTracking().FirstOrDefaultAsync(l => l.LibraryServiceId == library.LibraryServiceId, ct);
        Assert.NotNull(updated);
        Assert.Equal("RenamedLibrary", updated.Name);
        Assert.Equal("original-api-key", updated.ApiKey);
    }

    [Fact]
    public async Task PatchLibrary_RotatesCredentialsWithApiKey()
    {
        using FakeKomgaServer server = new(HttpStatusCode.OK, FakeKomgaServer.EmptySeriesListResponseBody);
        await using LibrariesContext context = LibrariesContextFactory.Create();
        DbLibraryService library = await SeedLibrary(context, server.BaseUrl, ct);

        PatchLibraryEndpoint.PatchLibraryRequest request = new() { ApiKey = "rotated-api-key" };

        Results<Ok, BadRequest<string>, NotFound> result = await PatchLibraryEndpoint.Handle(context, library.LibraryServiceId, request, ct);

        Assert.IsType<Ok>(result.Result);
        DbLibraryService? updated = await context.LibraryServices.AsNoTracking().FirstOrDefaultAsync(l => l.LibraryServiceId == library.LibraryServiceId, ct);
        Assert.NotNull(updated);
        Assert.Equal("rotated-api-key", updated.ApiKey);
    }

    [Fact]
    public async Task PatchLibrary_RotatesCredentialsWithUsernameAndPassword()
    {
        using FakeKomgaServer server = new(HttpStatusCode.OK, FakeKomgaServer.ValidApiKeyMintResponseBody);
        await using LibrariesContext context = LibrariesContextFactory.Create();
        DbLibraryService library = await SeedLibrary(context, server.BaseUrl, ct);

        PatchLibraryEndpoint.PatchLibraryRequest request = new() { Username = "newuser", Password = "newpassword" };

        Results<Ok, BadRequest<string>, NotFound> result = await PatchLibraryEndpoint.Handle(context, library.LibraryServiceId, request, ct);

        Assert.IsType<Ok>(result.Result);
        DbLibraryService? updated = await context.LibraryServices.AsNoTracking().FirstOrDefaultAsync(l => l.LibraryServiceId == library.LibraryServiceId, ct);
        Assert.NotNull(updated);
        Assert.Equal("minted-api-key-value", updated.ApiKey);
        Assert.Equal("newuser", updated.Username);
    }

    [Fact]
    public async Task PatchLibrary_RejectsInvalidCredentialsWithoutModifyingLibrary()
    {
        using FakeKomgaServer server = new(HttpStatusCode.Unauthorized);
        await using LibrariesContext context = LibrariesContextFactory.Create();
        DbLibraryService library = await SeedLibrary(context, server.BaseUrl, ct);

        PatchLibraryEndpoint.PatchLibraryRequest request = new() { Username = "newuser", Password = "wrongpassword" };

        Results<Ok, BadRequest<string>, NotFound> result = await PatchLibraryEndpoint.Handle(context, library.LibraryServiceId, request, ct);

        Assert.IsType<BadRequest<string>>(result.Result);
        DbLibraryService? unchanged = await context.LibraryServices.AsNoTracking().FirstOrDefaultAsync(l => l.LibraryServiceId == library.LibraryServiceId, ct);
        Assert.NotNull(unchanged);
        Assert.Equal("original-api-key", unchanged.ApiKey);
    }

    [Fact]
    public async Task PatchLibrary_Returns404ForUnknownId()
    {
        await using LibrariesContext context = LibrariesContextFactory.Create();

        PatchLibraryEndpoint.PatchLibraryRequest request = new() { Name = "RenamedLibrary" };

        Results<Ok, BadRequest<string>, NotFound> result = await PatchLibraryEndpoint.Handle(context, Guid.NewGuid(), request, ct);

        Assert.IsType<NotFound>(result.Result);
    }
}
