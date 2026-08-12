using System.Security.Cryptography;
using System.Text;
using Common.Settings;
using Microsoft.IdentityModel.Tokens;

namespace Common.Services.Authentication;

/// <summary>Derives the symmetric key used to sign/verify session JWTs from <see cref="EnvVars.AuthSigningKey"/>.</summary>
public static class JwtSigningKeyProvider
{
    /// <summary>
    /// Returns the signing key. The configured secret is hashed to a fixed 32-byte key so any non-empty
    /// operator-supplied string is usable, regardless of its own length.
    /// </summary>
    public static SymmetricSecurityKey GetKey()
    {
        string configured = EnvVars.AuthSigningKey ??
            throw new Exception("Missing required EnvVar 'AUTH_SIGNING_KEY' (required when 'UseAuth' is enabled)");

        byte[] keyBytes = SHA256.HashData(Encoding.UTF8.GetBytes(configured));
        return new SymmetricSecurityKey(keyBytes);
    }
}
