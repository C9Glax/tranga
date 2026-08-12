using System.Security.Claims;
using System.Text.Encodings.Web;
using Common.Database.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Common.Services.Authentication;

/// <summary>
/// Authenticates requests carrying an <c>X-Api-Key</c> header by hashing the presented key and looking it up
/// in <see cref="AuthContext.ApiKeys"/>. Falls through with <see cref="AuthenticateResult.NoResult"/> when the
/// header is absent, so the policy scheme in <see cref="JwtAuthenticationExtensions"/> can fall back to it only
/// when a bearer token isn't present either.
/// </summary>
public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    AuthContext authContext)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    /// <inheritdoc />
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyAuthenticationDefaults.HeaderName, out StringValues headerValues))
            return AuthenticateResult.NoResult();

        string? providedKey = headerValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(providedKey))
            return AuthenticateResult.NoResult();

        string hash = ApiKeyHasher.Hash(providedKey);
        DbApiKey? apiKey = await authContext.ApiKeys.FirstOrDefaultAsync(k => k.KeyHash == hash);
        if (apiKey is null)
            return AuthenticateResult.Fail("Invalid API key");

        await authContext.ApiKeys
            .Where(k => k.Id == apiKey.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(k => k.LastUsedAt, DateTimeOffset.UtcNow));

        ClaimsIdentity identity = new(
        [
            new Claim(ClaimTypes.NameIdentifier, apiKey.Id.ToString()),
            new Claim("scope", apiKey.Scope.ToString())
        ], Scheme.Name);

        AuthenticationTicket ticket = new(new ClaimsPrincipal(identity), Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }
}
