using System.Net;
using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Services.Libraries.Features.Libraries;
using Services.Libraries.Tests.Helpers;

namespace Services.Libraries.Tests.Features.Libraries;

public sealed class TestKomgaConnectionEndpointTests : TrangaTest
{
    [Fact]
    public async Task TestConnection_SucceedsWithValidApiKey()
    {
        using FakeKomgaServer server = new(HttpStatusCode.OK, FakeKomgaServer.EmptySeriesListResponseBody);

        TestKomgaConnectionEndpoint.TestKomgaConnectionRequest request = new()
        {
            BaseUrl = server.BaseUrl,
            ApiKey = "some-api-key"
        };

        Results<Ok, BadRequest<string>, UnauthorizedHttpResult> result = await TestKomgaConnectionEndpoint.Handle(request, ct);

        Assert.IsType<Ok>(result.Result);
    }

    [Fact]
    public async Task TestConnection_SucceedsWithValidUsernameAndPassword()
    {
        using FakeKomgaServer server = new(HttpStatusCode.OK, FakeKomgaServer.ValidApiKeyMintResponseBody);

        TestKomgaConnectionEndpoint.TestKomgaConnectionRequest request = new()
        {
            BaseUrl = server.BaseUrl,
            Username = "someuser",
            Password = "somepassword"
        };

        Results<Ok, BadRequest<string>, UnauthorizedHttpResult> result = await TestKomgaConnectionEndpoint.Handle(request, ct);

        Assert.IsType<Ok>(result.Result);
    }

    [Fact]
    public async Task TestConnection_FailsWithInvalidApiKey()
    {
        using FakeKomgaServer server = new(HttpStatusCode.Unauthorized);

        TestKomgaConnectionEndpoint.TestKomgaConnectionRequest request = new()
        {
            BaseUrl = server.BaseUrl,
            ApiKey = "wrong-api-key"
        };

        Results<Ok, BadRequest<string>, UnauthorizedHttpResult> result = await TestKomgaConnectionEndpoint.Handle(request, ct);

        Assert.IsType<UnauthorizedHttpResult>(result.Result);
    }

    [Fact]
    public async Task TestConnection_FailsWithInvalidCredentials()
    {
        using FakeKomgaServer server = new(HttpStatusCode.Unauthorized);

        TestKomgaConnectionEndpoint.TestKomgaConnectionRequest request = new()
        {
            BaseUrl = server.BaseUrl,
            Username = "someuser",
            Password = "wrongpassword"
        };

        Results<Ok, BadRequest<string>, UnauthorizedHttpResult> result = await TestKomgaConnectionEndpoint.Handle(request, ct);

        Assert.IsType<UnauthorizedHttpResult>(result.Result);
    }

    [Fact]
    public async Task TestConnection_RejectsWhenNeitherAuthModeGiven()
    {
        TestKomgaConnectionEndpoint.TestKomgaConnectionRequest request = new()
        {
            BaseUrl = "http://localhost:8080"
        };

        Results<Ok, BadRequest<string>, UnauthorizedHttpResult> result = await TestKomgaConnectionEndpoint.Handle(request, ct);

        Assert.IsType<BadRequest<string>>(result.Result);
    }

    [Fact]
    public async Task TestConnection_RejectsWhenBothAuthModesGiven()
    {
        TestKomgaConnectionEndpoint.TestKomgaConnectionRequest request = new()
        {
            BaseUrl = "http://localhost:8080",
            ApiKey = "some-api-key",
            Username = "someuser",
            Password = "somepassword"
        };

        Results<Ok, BadRequest<string>, UnauthorizedHttpResult> result = await TestKomgaConnectionEndpoint.Handle(request, ct);

        Assert.IsType<BadRequest<string>>(result.Result);
    }
}
