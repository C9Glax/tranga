using Common.Database.Auth;
using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Services.Auth.Features.Auth;
using Services.Auth.Tests.Helpers;

namespace Services.Auth.Tests.Features.Auth;

public class PostSetupEndpointTests : TrangaTest
{
    [Fact]
    public async Task Setup_CreatesCredential_AndReturnsToken()
    {
        await using AuthContext context = AuthContextFactory.Create();

        Results<Ok<AuthTokenResponse>, Conflict, BadRequest<string>> result =
            await PostSetupEndpoint.Handle(context, new SetupRequest("a-decent-password"), ct);

        Ok<AuthTokenResponse> ok = Assert.IsType<Ok<AuthTokenResponse>>(result.Result);
        Assert.False(string.IsNullOrWhiteSpace(ok.Value!.Token));
        Assert.True(await context.Credentials.AnyAsync(ct));
    }

    [Fact]
    public async Task Setup_Returns409_WhenAlreadyConfigured()
    {
        await using AuthContext context = AuthContextFactory.Create();
        await PostSetupEndpoint.Handle(context, new SetupRequest("first-password"), ct);

        Results<Ok<AuthTokenResponse>, Conflict, BadRequest<string>> result =
            await PostSetupEndpoint.Handle(context, new SetupRequest("second-password"), ct);

        Assert.IsType<Conflict>(result.Result);
    }

    [Fact]
    public async Task Setup_Returns400_ForTooShortPassword()
    {
        await using AuthContext context = AuthContextFactory.Create();

        Results<Ok<AuthTokenResponse>, Conflict, BadRequest<string>> result =
            await PostSetupEndpoint.Handle(context, new SetupRequest("short"), ct);

        Assert.IsType<BadRequest<string>>(result.Result);
        Assert.False(await context.Credentials.AnyAsync(ct));
    }
}
