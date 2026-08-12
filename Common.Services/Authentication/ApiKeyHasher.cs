using System.Security.Cryptography;
using System.Text;

namespace Common.Services.Authentication;

/// <summary>
/// Generates/hashes API keys with a plain SHA-256 hash, unsalted and fast. That is intentional: unlike the
/// admin password, an API key is 32 bytes of true randomness, so brute-forcing the hash is infeasible
/// regardless of hash speed - and a fast, unsalted hash allows an indexed exact-match lookup in
/// <c>AuthContext.ApiKeys</c> instead of iterating every stored key to re-run a slow KDF against each one.
/// </summary>
public static class ApiKeyHasher
{
    private const string Prefix = "tga_";

    /// <summary>Generates a new random raw API key (shown to the caller exactly once).</summary>
    public static string GenerateKey()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(32);
        return Prefix + Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>Hashes a raw key for storage/lookup.</summary>
    public static string Hash(string rawKey) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey)));
}
