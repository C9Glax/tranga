using Common.Database.Auth;
using Common.Services.Authentication;
using Common.Tests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
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

        Results<Ok<AuthTokenResponse>, UnauthorizedHttpResult, ContentHttpResult> result =
            await PostLoginEndpoint.Handle(context, new SetupRequest("my-password"), ct);

        Assert.IsType<Ok<AuthTokenResponse>>(result.Result);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        await using AuthContext context = AuthContextFactory.Create();
        await context.Credentials.AddAsync(new DbCredential { PasswordHash = PasswordHasher.Hash("my-password") }, ct);
        await context.SaveChangesAsync(ct);

        Results<Ok<AuthTokenResponse>, UnauthorizedHttpResult, ContentHttpResult> result =
            await PostLoginEndpoint.Handle(context, new SetupRequest("wrong-password"), ct);

        Assert.IsType<UnauthorizedHttpResult>(result.Result);
    }

    [Fact]
    public async Task Login_BeforeSetup_ReturnsUnauthorized()
    {
        await using AuthContext context = AuthContextFactory.Create();

        Results<Ok<AuthTokenResponse>, UnauthorizedHttpResult, ContentHttpResult> result =
            await PostLoginEndpoint.Handle(context, new SetupRequest("anything"), ct);

        Assert.IsType<UnauthorizedHttpResult>(result.Result);
    }

    [Fact]
    public async Task Login_BelowLockoutThreshold_StillReturnsUnauthorized()
    {
        await using AuthContext context = AuthContextFactory.Create();
        await context.Credentials.AddAsync(new DbCredential { PasswordHash = PasswordHasher.Hash("my-password") }, ct);
        await context.SaveChangesAsync(ct);

        for (int i = 0; i < LoginLockoutPolicy.Threshold - 1; i++)
        {
            Results<Ok<AuthTokenResponse>, UnauthorizedHttpResult, ContentHttpResult> result =
                await PostLoginEndpoint.Handle(context, new SetupRequest("wrong-password"), ct);
            Assert.IsType<UnauthorizedHttpResult>(result.Result);
        }
    }

    [Fact]
    public async Task Login_ReachingLockoutThreshold_ReturnsTooManyRequests()
    {
        await using AuthContext context = AuthContextFactory.Create();
        await context.Credentials.AddAsync(new DbCredential { PasswordHash = PasswordHasher.Hash("my-password") }, ct);
        await context.SaveChangesAsync(ct);

        Results<Ok<AuthTokenResponse>, UnauthorizedHttpResult, ContentHttpResult> result = default!;
        for (int i = 0; i < LoginLockoutPolicy.Threshold; i++)
            result = await PostLoginEndpoint.Handle(context, new SetupRequest("wrong-password"), ct);

        ContentHttpResult content = Assert.IsType<ContentHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status429TooManyRequests, content.StatusCode);
    }

    [Fact]
    public async Task Login_WhileLocked_RejectsEvenTheCorrectPassword()
    {
        await using AuthContext context = AuthContextFactory.Create();
        await context.Credentials.AddAsync(new DbCredential { PasswordHash = PasswordHasher.Hash("my-password") }, ct);
        await context.SaveChangesAsync(ct);

        for (int i = 0; i < LoginLockoutPolicy.Threshold; i++)
            await PostLoginEndpoint.Handle(context, new SetupRequest("wrong-password"), ct);

        Results<Ok<AuthTokenResponse>, UnauthorizedHttpResult, ContentHttpResult> result =
            await PostLoginEndpoint.Handle(context, new SetupRequest("my-password"), ct);

        ContentHttpResult content = Assert.IsType<ContentHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status429TooManyRequests, content.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectPassword_ResetsFailedAttemptsAndLockout()
    {
        await using AuthContext context = AuthContextFactory.Create();
        await context.Credentials.AddAsync(
            new DbCredential
            {
                PasswordHash = PasswordHasher.Hash("my-password"),
                FailedLoginAttempts = LoginLockoutPolicy.Threshold - 1,
            },
            ct);
        await context.SaveChangesAsync(ct);

        Results<Ok<AuthTokenResponse>, UnauthorizedHttpResult, ContentHttpResult> result =
            await PostLoginEndpoint.Handle(context, new SetupRequest("my-password"), ct);

        Assert.IsType<Ok<AuthTokenResponse>>(result.Result);
        DbCredential updated = await context.Credentials.SingleAsync(ct);
        Assert.Equal(0, updated.FailedLoginAttempts);
        Assert.Null(updated.LockedUntil);
    }
}
