using Common.Database.Auth;
using Common.Services.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Services.Auth.Features.Auth;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class PostLoginEndpoint
{
    /// <summary>
    /// Logs in with the admin password, returning a session token.
    /// </summary>
    /// <param name="authContext"></param>
    /// <param name="req"></param>
    /// <param name="ct"></param>
    /// <response code="200">Password matched; returns a session token</response>
    /// <response code="401">No password set up yet, or the password did not match</response>
    public static async Task<Results<Ok<AuthTokenResponse>, UnauthorizedHttpResult>> Handle(
        AuthContext authContext, [FromBody] SetupRequest req, CancellationToken ct)
    {
        DbCredential? credential = await authContext.Credentials.SingleOrDefaultAsync(ct);
        if (credential is null || !PasswordHasher.Verify(req.Password, credential.PasswordHash))
            return TypedResults.Unauthorized();

        return TypedResults.Ok(new AuthTokenResponse(JwtTokenService.CreateToken()));
    }
}
