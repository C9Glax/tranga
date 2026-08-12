using Common.Database.Auth;
using Common.Services.Authentication;
using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Services.Auth.Features.ApiKeys;
using Services.Auth.Tests.Helpers;

namespace Services.Auth.Tests.Features.ApiKeys;

public class PostApiKeyEndpointTests : TrangaTest
{
    [Fact]
    public async Task CreateApiKey_ReturnsRawKeyOnce_AndStoresOnlyItsHash()
    {
        await using AuthContext context = AuthContextFactory.Create();

        Ok<CreateApiKeyResponse> result = await PostApiKeyEndpoint.Handle(
            context, new CreateApiKeyRequest("my-key", ApiKeyScope.All), ct);

        CreateApiKeyResponse response = result.Value!;
        Assert.StartsWith("tga_", response.Key);
        Assert.Equal("my-key", response.Name);

        DbApiKey stored = await context.ApiKeys.SingleAsync(ct);
        Assert.Equal(ApiKeyHasher.Hash(response.Key), stored.KeyHash);
        Assert.NotEqual(response.Key, stored.KeyHash);
    }
}
