using Common.Database.Auth;
using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Services.Auth.Features.ApiKeys;
using Services.Auth.Tests.Helpers;

namespace Services.Auth.Tests.Features.ApiKeys;

public class GetApiKeysEndpointTests : TrangaTest
{
    [Fact]
    public async Task ListApiKeys_ReturnsMetadataForEachKey()
    {
        await using AuthContext context = AuthContextFactory.Create();
        await PostApiKeyEndpoint.Handle(context, new CreateApiKeyRequest("visible-key", ApiKeyScope.All), ct);

        Ok<List<ApiKeyResponse>> result = await GetApiKeysEndpoint.Handle(context, ct);

        ApiKeyResponse entry = Assert.Single(result.Value!);
        Assert.Equal("visible-key", entry.Name);
        Assert.Equal(ApiKeyScope.All, entry.Scope);
    }

    [Fact]
    public async Task ListApiKeys_ReturnsEmpty_WhenNoneExist()
    {
        await using AuthContext context = AuthContextFactory.Create();

        Ok<List<ApiKeyResponse>> result = await GetApiKeysEndpoint.Handle(context, ct);

        Assert.Empty(result.Value!);
    }
}
