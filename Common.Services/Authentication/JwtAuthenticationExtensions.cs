using Common.Database.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Common.Services.Authentication;

/// <summary>
/// Registers the two ways a request can be authenticated - a session JWT (<c>Authorization: Bearer</c>,
/// verified locally against the shared signing key, no DB lookup) or an API key (<c>X-Api-Key</c>, verified
/// against <see cref="AuthContext"/>) - behind a single policy scheme so <c>.RequireAuthorization()</c> accepts
/// either.
/// </summary>
public static class JwtAuthenticationExtensions
{
    /// <summary>Name of the combined scheme registered as the default authentication/challenge scheme.</summary>
    public const string PolicySchemeName = "TrangaAuth";

    /// <summary>Registers JWT-bearer and API-key authentication behind <see cref="PolicySchemeName"/>, and authorization.</summary>
    public static IServiceCollection AddTrangaAuthentication(this IServiceCollection services)
    {
        services.AddAuthContextIfMissing();

        services.AddAuthentication(PolicySchemeName)
            .AddPolicyScheme(PolicySchemeName, PolicySchemeName, options =>
            {
                options.ForwardDefaultSelector = context =>
                    context.Request.Headers.ContainsKey(ApiKeyAuthenticationDefaults.HeaderName)
                        ? ApiKeyAuthenticationDefaults.SchemeName
                        : JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = JwtTokenService.Issuer,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = JwtSigningKeyProvider.GetKey()
                };
            })
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationDefaults.SchemeName, _ => { });

        services.AddAuthorization();

        return services;
    }
}
