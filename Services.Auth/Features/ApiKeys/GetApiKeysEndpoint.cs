using Common.Database.Auth;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Services.Auth.Features.ApiKeys;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class GetApiKeysEndpoint
{
    /// <summary>
    /// Lists every API key's metadata. The raw secret and its hash are never included - only <see cref="PostApiKeyEndpoint"/>'s
    /// response ever contains the raw key.
    /// </summary>
    /// <param name="authContext"></param>
    /// <param name="ct"></param>
    /// <response code="200">Metadata for every configured API key</response>
    public static async Task<Ok<List<ApiKeyResponse>>> Handle(AuthContext authContext, CancellationToken ct)
    {
        // Sorted client-side: Sqlite (used in tests) can't translate ORDER BY on DateTimeOffset, and the
        // number of API keys for a single-admin deployment is always small enough that this is irrelevant.
        List<ApiKeyResponse> keys = await authContext.ApiKeys
            .Select(k => new ApiKeyResponse(k.Id, k.Name, k.Scope, k.CreatedAt, k.LastUsedAt))
            .ToListAsync(ct);

        return TypedResults.Ok(keys.OrderByDescending(k => k.CreatedAt).ToList());
    }
}

/// <summary>Used in <see cref="GetApiKeysEndpoint"/>.</summary>
public sealed record ApiKeyResponse(Guid Id, string? Name, ApiKeyScope Scope, DateTimeOffset CreatedAt, DateTimeOffset? LastUsedAt);
