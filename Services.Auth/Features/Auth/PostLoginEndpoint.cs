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
    /// Logs in with the admin password, returning a session token. Failed attempts are tracked on the
    /// credential itself; after <see cref="LoginLockoutPolicy.Threshold"/> consecutive failures, further
    /// attempts are rejected without checking the password until the escalating lockout window passes.
    /// </summary>
    /// <param name="authContext"></param>
    /// <param name="req"></param>
    /// <param name="ct"></param>
    /// <response code="200">Password matched; returns a session token</response>
    /// <response code="401">No password set up yet, or the password did not match</response>
    /// <response code="429">Too many failed attempts; the response body says how long to wait</response>
    public static async Task<Results<Ok<AuthTokenResponse>, UnauthorizedHttpResult, ContentHttpResult>> Handle(
        AuthContext authContext, [FromBody] SetupRequest req, CancellationToken ct)
    {
        DbCredential? credential = await authContext.Credentials.SingleOrDefaultAsync(ct);
        if (credential is null)
            return TypedResults.Unauthorized();

        DateTimeOffset now = DateTimeOffset.UtcNow;

        if (credential.LockedUntil is { } lockedUntil && lockedUntil > now)
            return LockedResult(lockedUntil - now);

        if (!PasswordHasher.Verify(req.Password, credential.PasswordHash))
        {
            int failedAttempts = credential.FailedLoginAttempts + 1;
            TimeSpan? lockout = LoginLockoutPolicy.ComputeLockout(failedAttempts);

            authContext.Entry(credential).CurrentValues.SetValues(credential with
            {
                FailedLoginAttempts = failedAttempts,
                LockedUntil = lockout is { } duration ? now + duration : null,
            });
            await authContext.SaveChangesAsync(ct);

            return lockout is { } locked ? LockedResult(locked) : TypedResults.Unauthorized();
        }

        authContext.Entry(credential).CurrentValues.SetValues(credential with
        {
            FailedLoginAttempts = 0,
            LockedUntil = null,
            UpdatedAt = now,
        });
        await authContext.SaveChangesAsync(ct);

        return TypedResults.Ok(new AuthTokenResponse(JwtTokenService.CreateToken()));
    }

    private static ContentHttpResult LockedResult(TimeSpan remaining) =>
        TypedResults.Text(
            $"Too many failed attempts. Try again in {LoginLockoutPolicy.Format(remaining)}.",
            statusCode: StatusCodes.Status429TooManyRequests);
}
