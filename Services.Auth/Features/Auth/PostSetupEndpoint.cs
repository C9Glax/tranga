using Common.Database.Auth;
using Common.Services.Authentication;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Services.Auth.Features.Auth;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class PostSetupEndpoint
{
    /// <summary>
    /// Creates the one admin password. Only works once - a password can't be replaced through this endpoint
    /// once one exists.
    /// </summary>
    /// <param name="authContext"></param>
    /// <param name="req"></param>
    /// <param name="ct"></param>
    /// <response code="200">Password created; returns a session token, logging the caller in</response>
    /// <response code="409">A password has already been set up</response>
    /// <response code="400">Password does not meet the minimum length requirement</response>
    public static async Task<Results<Ok<AuthTokenResponse>, Conflict, BadRequest<string>>> Handle(
        AuthContext authContext, [FromBody] SetupRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Password) || req.Password.Length < PasswordHasher.MinPasswordLength)
            return TypedResults.BadRequest($"Password must be at least {PasswordHasher.MinPasswordLength} characters.");

        if (await authContext.Credentials.AnyAsync(ct))
            return TypedResults.Conflict();

        DbCredential credential = new() { PasswordHash = PasswordHasher.Hash(req.Password) };
        await authContext.Credentials.AddAsync(credential, ct);

        try
        {
            await authContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Another request set up the password between our check and this insert.
            return TypedResults.Conflict();
        }

        return TypedResults.Ok(new AuthTokenResponse(JwtTokenService.CreateToken()));
    }
}

/// <summary>Used in <see cref="PostSetupEndpoint"/> and <see cref="PostLoginEndpoint"/>.</summary>
/// <param name="Password">The admin password, in plaintext over the request body (hashed before storage).</param>
public sealed record SetupRequest(string Password);

/// <summary>Used in <see cref="PostSetupEndpoint"/> and <see cref="PostLoginEndpoint"/>.</summary>
/// <param name="Token">Signed session JWT; send as <c>Authorization: Bearer &lt;token&gt;</c> on subsequent requests.</param>
public sealed record AuthTokenResponse(string Token);
