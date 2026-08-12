namespace Common.Database.Auth;

/// <summary>
/// A scoped API key. The raw secret is shown exactly once at creation time and never persisted or retrievable
/// again - only its <see cref="KeyHash"/> is stored, for lookup during request validation.
/// </summary>
public sealed record DbApiKey
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    /// <summary>Optional operator-chosen label, shown in the key-management UI.</summary>
    public string? Name { get; init; }

    /// <summary>SHA-256 hash of the raw key. Fast/unsalted is intentional: the key itself is 32 bytes of random
    /// data, so brute-forcing the hash is infeasible regardless of hash speed, and this allows an indexed
    /// exact-match lookup instead of iterating every stored key (unlike the password, which is low-entropy and
    /// uses a slow, salted KDF).</summary>
    public string KeyHash { get; init; } = string.Empty;

    public ApiKeyScope Scope { get; init; } = ApiKeyScope.All;

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? LastUsedAt { get; init; }
}
