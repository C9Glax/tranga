using Common.Database.Auth;
using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Services.Auth.Features.Auth;
using Services.Auth.Tests.Helpers;

namespace Services.Auth.Tests.Features.Auth;

public class GetStatusEndpointTests : TrangaTest
{
    [Fact]
    public async Task GetStatus_ReportsNotConfigured_WhenNoCredentialExists()
    {
        await using AuthContext context = AuthContextFactory.Create();

        Ok<AuthStatusResponse> result = await GetStatusEndpoint.Handle(context, ct);

        Assert.False(result.Value!.Configured);
    }

    [Fact]
    public async Task GetStatus_ReportsConfigured_AfterCredentialExists()
    {
        await using AuthContext context = AuthContextFactory.Create();
        await context.Credentials.AddAsync(new DbCredential { PasswordHash = "irrelevant" }, ct);
        await context.SaveChangesAsync(ct);

        Ok<AuthStatusResponse> result = await GetStatusEndpoint.Handle(context, ct);

        Assert.True(result.Value!.Configured);
    }
}
