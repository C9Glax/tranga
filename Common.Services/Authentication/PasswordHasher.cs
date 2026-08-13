using System.Security.Cryptography;

namespace Common.Services.Authentication;

/// <summary>
/// Hashes/verifies the single admin password with PBKDF2 - slow and salted, appropriate for a low-entropy,
/// human-chosen secret. API keys use <see cref="ApiKeyHasher"/> instead; see its doc comment for why.
/// </summary>
public static class PasswordHasher
{
    /// <summary>Minimum accepted length for the admin password, enforced wherever it's set.</summary>
    public const int MinPasswordLength = 8;

    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 210_000;

    /// <summary>Hashes <paramref name="password"/>, returning <c>"{iterations}.{saltBase64}.{hashBase64}"</c>.</summary>
    public static string Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    /// <summary>Verifies <paramref name="password"/> against a hash produced by <see cref="Hash"/>.</summary>
    public static bool Verify(string password, string stored)
    {
        string[] parts = stored.Split('.', 3);
        if (parts.Length != 3 || !int.TryParse(parts[0], out int iterations))
            return false;

        byte[] salt = Convert.FromBase64String(parts[1]);
        byte[] expectedHash = Convert.FromBase64String(parts[2]);
        byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
