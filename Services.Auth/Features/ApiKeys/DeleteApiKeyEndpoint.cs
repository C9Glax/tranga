using Common.Database.Auth;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Services.Auth.Features.ApiKeys;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class DeleteApiKeyEndpoint
{
    /// <summary>
    /// Revokes an API key; any request using it is rejected from then on.
    /// </summary>
    /// <param name="authContext"></param>
    /// <param name="apiKeyId">ID of the API key to revoke</param>
    /// <param name="ct"></param>
    /// <response code="200">Key revoked</response>
    /// <response code="404">API key with that ID does not exist</response>
    public static async Task<Results<Ok, NotFound>> Handle(AuthContext authContext, [FromRoute] Guid apiKeyId, CancellationToken ct)
    {
        if (await authContext.ApiKeys.Where(k => k.Id == apiKeyId).ExecuteDeleteAsync(ct) < 1)
            return TypedResults.NotFound();

        return TypedResults.Ok();
    }
}
