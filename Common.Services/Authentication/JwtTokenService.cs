using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Common.Services.Authentication;

/// <summary>Issues the session JWT returned by <c>/auth/setup</c> and <c>/auth/login</c>.</summary>
public static class JwtTokenService
{
    /// <summary>The <c>iss</c> claim set on every issued token.</summary>
    public const string Issuer = "Tranga";

    /// <summary>How long an issued session token remains valid.</summary>
    public static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(12);

    /// <summary>Creates a signed session token for the (single) admin.</summary>
    public static string CreateToken()
    {
        SigningCredentials credentials = new(JwtSigningKeyProvider.GetKey(), SecurityAlgorithms.HmacSha256);
        JwtSecurityToken token = new(
            issuer: Issuer,
            claims: [new Claim(ClaimTypes.NameIdentifier, "admin")],
            expires: DateTime.UtcNow.Add(TokenLifetime),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
