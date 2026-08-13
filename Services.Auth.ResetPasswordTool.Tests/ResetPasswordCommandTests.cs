using Common.Database.Auth;
using Common.Services.Authentication;
using Common.Tests;
using Microsoft.EntityFrameworkCore;
using Services.Auth.ResetPasswordTool.Tests.Helpers;

namespace Services.Auth.ResetPasswordTool.Tests;

public class ResetPasswordCommandTests : TrangaTest
{
    [Fact]
    public async Task SetPasswordAsync_WithNoExistingCredential_CreatesOne()
    {
        await using AuthContext context = AuthContextFactory.Create();

        await ResetPasswordCommand.SetPasswordAsync(context, "new-password", ct);

        DbCredential credential = await context.Credentials.SingleAsync(ct);
        Assert.True(PasswordHasher.Verify("new-password", credential.PasswordHash));
    }

    [Fact]
    public async Task SetPasswordAsync_WithExistingCredential_OverwritesPassword()
    {
        await using AuthContext context = AuthContextFactory.Create();
        await context.Credentials.AddAsync(new DbCredential { PasswordHash = PasswordHasher.Hash("old-password") }, ct);
        await context.SaveChangesAsync(ct);

        await ResetPasswordCommand.SetPasswordAsync(context, "new-password", ct);

        DbCredential credential = await context.Credentials.SingleAsync(ct);
        Assert.True(PasswordHasher.Verify("new-password", credential.PasswordHash));
        Assert.False(PasswordHasher.Verify("old-password", credential.PasswordHash));
    }

    [Fact]
    public async Task SetPasswordAsync_ClearsExistingLockout()
    {
        await using AuthContext context = AuthContextFactory.Create();
        await context.Credentials.AddAsync(
            new DbCredential
            {
                PasswordHash = PasswordHasher.Hash("old-password"),
                FailedLoginAttempts = 5,
                LockedUntil = DateTimeOffset.UtcNow.AddHours(1),
            },
            ct);
        await context.SaveChangesAsync(ct);

        await ResetPasswordCommand.SetPasswordAsync(context, "new-password", ct);

        DbCredential credential = await context.Credentials.SingleAsync(ct);
        Assert.Equal(0, credential.FailedLoginAttempts);
        Assert.Null(credential.LockedUntil);
    }

    [Fact]
    public async Task RemoveCredentialAsync_WithExistingCredential_DeletesIt()
    {
        await using AuthContext context = AuthContextFactory.Create();
        await context.Credentials.AddAsync(new DbCredential { PasswordHash = PasswordHasher.Hash("some-password") }, ct);
        await context.SaveChangesAsync(ct);

        await ResetPasswordCommand.RemoveCredentialAsync(context, ct);

        Assert.False(await context.Credentials.AnyAsync(ct));
    }

    [Fact]
    public async Task RemoveCredentialAsync_WithNoExistingCredential_DoesNotThrow()
    {
        await using AuthContext context = AuthContextFactory.Create();

        await ResetPasswordCommand.RemoveCredentialAsync(context, ct);

        Assert.False(await context.Credentials.AnyAsync(ct));
    }
}
