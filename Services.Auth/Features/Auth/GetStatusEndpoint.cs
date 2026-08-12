using Common.Database.Auth;
using Common.Settings;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Services.Auth.Features.Auth;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class GetStatusEndpoint
{
    /// <summary>
    /// Reports whether authorization is enabled for this deployment and whether the admin password has been
    /// created yet. Always reachable, even when authorization is enabled, so the frontend can decide whether to
    /// show the setup or login screen before it has a token.
    /// </summary>
    /// <param name="authContext"></param>
    /// <param name="ct"></param>
    /// <response code="200">Current auth status</response>
    public static async Task<Ok<AuthStatusResponse>> Handle(AuthContext authContext, CancellationToken ct)
    {
        bool configured = await authContext.Credentials.AnyAsync(ct);
        return TypedResults.Ok(new AuthStatusResponse(EnvVars.UseAuth, configured));
    }
}

/// <summary>Used in <see cref="GetStatusEndpoint"/></summary>
/// <param name="Enabled">Whether <c>UseAuth</c> is turned on for this deployment.</param>
/// <param name="Configured">Whether the admin password has already been created.</param>
public sealed record AuthStatusResponse(bool Enabled, bool Configured);
