using Common.Database.Auth;
using Common.Services.Authentication;
using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Services.Auth.Features.Auth;
using Services.Auth.Tests.Helpers;

namespace Services.Auth.Tests.Features.Auth;

public class PostLoginEndpointTests : TrangaTest
{
    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsToken()
    {
        await using AuthContext context = AuthContextFactory.Create();
        await context.Credentials.AddAsync(new DbCredential { PasswordHash = PasswordHasher.Hash("my-password") }, ct);
        await context.SaveChangesAsync(ct);

        Results<Ok<AuthTokenResponse>, UnauthorizedHttpResult> result =
            await PostLoginEndpoint.Handle(context, new SetupRequest("my-password"), ct);

        Assert.IsType<Ok<AuthTokenResponse>>(result.Result);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        await using AuthContext context = AuthContextFactory.Create();
        await context.Credentials.AddAsync(new DbCredential { PasswordHash = PasswordHasher.Hash("my-password") }, ct);
        await context.SaveChangesAsync(ct);

        Results<Ok<AuthTokenResponse>, UnauthorizedHttpResult> result =
            await PostLoginEndpoint.Handle(context, new SetupRequest("wrong-password"), ct);

        Assert.IsType<UnauthorizedHttpResult>(result.Result);
    }

    [Fact]
    public async Task Login_BeforeSetup_ReturnsUnauthorized()
    {
        await using AuthContext context = AuthContextFactory.Create();

        Results<Ok<AuthTokenResponse>, UnauthorizedHttpResult> result =
            await PostLoginEndpoint.Handle(context, new SetupRequest("anything"), ct);

        Assert.IsType<UnauthorizedHttpResult>(result.Result);
    }
}
