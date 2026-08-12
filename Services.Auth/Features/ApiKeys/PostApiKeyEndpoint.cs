using Common.Database.Auth;
using Common.Services.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Services.Auth.Features.ApiKeys;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class PostApiKeyEndpoint
{
    /// <summary>
    /// Creates a new API key. The raw key is returned exactly once in this response and is never retrievable
    /// again - only its hash is stored.
    /// </summary>
    /// <param name="authContext"></param>
    /// <param name="req"></param>
    /// <param name="ct"></param>
    /// <response code="200">The new key, metadata, and the one-time raw secret</response>
    public static async Task<Ok<CreateApiKeyResponse>> Handle(
        AuthContext authContext, [FromBody] CreateApiKeyRequest req, CancellationToken ct)
    {
        string rawKey = ApiKeyHasher.GenerateKey();
        DbApiKey apiKey = new()
        {
            Name = req.Name,
            Scope = req.Scope,
            KeyHash = ApiKeyHasher.Hash(rawKey)
        };

        await authContext.ApiKeys.AddAsync(apiKey, ct);
        await authContext.SaveChangesAsync(ct);

        return TypedResults.Ok(new CreateApiKeyResponse(apiKey.Id, rawKey, apiKey.Name, apiKey.Scope, apiKey.CreatedAt));
    }
}

/// <summary>Used in <see cref="PostApiKeyEndpoint"/>.</summary>
/// <param name="Name">Optional operator-chosen label.</param>
/// <param name="Scope">Access scope to grant the key.</param>
public sealed record CreateApiKeyRequest(string? Name, ApiKeyScope Scope = ApiKeyScope.All);

/// <summary>Used in <see cref="PostApiKeyEndpoint"/>.</summary>
/// <param name="Id">Id of the new key, used to revoke it later via <see cref="DeleteApiKeyEndpoint"/>.</param>
/// <param name="Key">The raw API key. Shown once - store it now, it cannot be retrieved again.</param>
/// <param name="Name">The label chosen for this key, if any.</param>
/// <param name="Scope">The access scope granted to this key.</param>
/// <param name="CreatedAt">When the key was created.</param>
public sealed record CreateApiKeyResponse(Guid Id, string Key, string? Name, ApiKeyScope Scope, DateTimeOffset CreatedAt);
