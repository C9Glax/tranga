using System.Net;
using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Services.Libraries.Database;
using Services.Libraries.Features.Libraries;
using Services.Libraries.Tests.Helpers;

namespace Services.Libraries.Tests.Features.Libraries;

public sealed class AddKomgaEndpointTests : TrangaTest
{
    [Fact]
    public async Task AddKomga_CreatesLibraryWithApiKey()
    {
        using FakeKomgaServer server = new(HttpStatusCode.OK, FakeKomgaServer.ValidLibraryCreationResponseBody);
        await using LibrariesContext context = LibrariesContextFactory.Create();

        AddKomgaEndpoint.AddKomgaLibraryRequest request = new()
        {
            Name = "MyLibrary",
            BaseUrl = server.BaseUrl,
            ApiKey = "some-api-key"
        };

        Results<Ok<Guid>, BadRequest<string>> result = await AddKomgaEndpoint.Handle(context, request, ct);

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
            : (HttpStatusCode.OK, FakeKomgaServer.ValidLibraryCreationResponseBody));
        await using LibrariesContext context = LibrariesContextFactory.Create();

        AddKomgaEndpoint.AddKomgaLibraryRequest request = new()
        {
            Name = "MyLibrary",
            BaseUrl = server.BaseUrl,
            Username = "someuser",
            Password = "somepassword"
        };

        Results<Ok<Guid>, BadRequest<string>> result = await AddKomgaEndpoint.Handle(context, request, ct);

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

        AddKomgaEndpoint.AddKomgaLibraryRequest request = new()
        {
            Name = "MyLibrary",
            BaseUrl = server.BaseUrl,
            Username = "someuser",
            Password = "wrongpassword"
        };

        Results<Ok<Guid>, BadRequest<string>> result = await AddKomgaEndpoint.Handle(context, request, ct);

        Assert.IsType<BadRequest<string>>(result.Result);
        Assert.Empty(await context.LibraryServices.ToListAsync(ct));
    }

    [Fact]
    public async Task AddKomga_RejectsWhenNeitherAuthModeGiven()
    {
        await using LibrariesContext context = LibrariesContextFactory.Create();

        AddKomgaEndpoint.AddKomgaLibraryRequest request = new()
        {
            Name = "MyLibrary",
            BaseUrl = "http://localhost:8080"
        };

        Results<Ok<Guid>, BadRequest<string>> result = await AddKomgaEndpoint.Handle(context, request, ct);

        Assert.IsType<BadRequest<string>>(result.Result);
        Assert.Empty(await context.LibraryServices.ToListAsync(ct));
    }

    [Fact]
    public async Task AddKomga_RejectsWhenBothAuthModesGiven()
    {
        await using LibrariesContext context = LibrariesContextFactory.Create();

        AddKomgaEndpoint.AddKomgaLibraryRequest request = new()
        {
            Name = "MyLibrary",
            BaseUrl = "http://localhost:8080",
            ApiKey = "some-api-key",
            Username = "someuser",
            Password = "somepassword"
        };

        Results<Ok<Guid>, BadRequest<string>> result = await AddKomgaEndpoint.Handle(context, request, ct);

        Assert.IsType<BadRequest<string>>(result.Result);
        Assert.Empty(await context.LibraryServices.ToListAsync(ct));
    }
}
