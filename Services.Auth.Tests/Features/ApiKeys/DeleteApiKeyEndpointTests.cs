using Common.Database.Auth;
using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Services.Auth.Features.ApiKeys;
using Services.Auth.Tests.Helpers;

namespace Services.Auth.Tests.Features.ApiKeys;

public class DeleteApiKeyEndpointTests : TrangaTest
{
    [Fact]
    public async Task DeleteApiKey_RemovesIt()
    {
        await using AuthContext context = AuthContextFactory.Create();
        Ok<CreateApiKeyResponse> created = await PostApiKeyEndpoint.Handle(context, new CreateApiKeyRequest(null, ApiKeyScope.All), ct);

        Results<Ok, NotFound> result = await DeleteApiKeyEndpoint.Handle(context, created.Value!.Id, ct);

        Assert.IsType<Ok>(result.Result);
        Assert.False(await context.ApiKeys.AnyAsync(ct));
    }

    [Fact]
    public async Task DeleteApiKey_Returns404_ForUnknownId()
    {
        await using AuthContext context = AuthContextFactory.Create();

        Results<Ok, NotFound> result = await DeleteApiKeyEndpoint.Handle(context, Guid.NewGuid(), ct);

        Assert.IsType<NotFound>(result.Result);
    }
}
