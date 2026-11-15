using System.Net;
using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Services.Libraries.Features.Libraries;
using Services.Libraries.Tests.Helpers;

namespace Services.Libraries.Tests.Features.Libraries;

public sealed class TestKavitaConnectionEndpointTests : TrangaTest
{
    [Fact]
    public async Task TestKavitaConnection_ReturnsLibrariesWhenAuthKeyValid()
    {
        using FakeKomgaServer server = new(HttpStatusCode.OK, """[{"id": 3, "name": "Manga"}]""");

        Results<Ok<TestKavitaConnectionEndpoint.KavitaLibraryInfo[]>, UnauthorizedHttpResult> result =
            await TestKavitaConnectionEndpoint.Handle(new TestKavitaConnectionEndpoint.TestKavitaConnectionRequest { BaseUrl = server.BaseUrl, ApiKey = "key" }, ct);

        TestKavitaConnectionEndpoint.KavitaLibraryInfo[] libraries = Assert.IsType<Ok<TestKavitaConnectionEndpoint.KavitaLibraryInfo[]>>(result.Result).Value!;
        Assert.Equal([new TestKavitaConnectionEndpoint.KavitaLibraryInfo("3", "Manga")], libraries);
    }

    [Fact]
    public async Task TestKavitaConnection_ReturnsUnauthorizedWhenAuthKeyInvalid()
    {
        using FakeKomgaServer server = new(HttpStatusCode.Unauthorized);

        Results<Ok<TestKavitaConnectionEndpoint.KavitaLibraryInfo[]>, UnauthorizedHttpResult> result =
            await TestKavitaConnectionEndpoint.Handle(new TestKavitaConnectionEndpoint.TestKavitaConnectionRequest { BaseUrl = server.BaseUrl, ApiKey = "bad" }, ct);

        Assert.IsType<UnauthorizedHttpResult>(result.Result);
    }
}
