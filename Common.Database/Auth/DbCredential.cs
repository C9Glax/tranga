namespace Common.Database.Auth;

/// <summary>
/// The single admin password. There is no user-profile system - this table has at most one row, created once
/// via the first-run setup flow.
/// </summary>
public sealed record DbCredential
{
    /// <summary>Fixed id every row uses - the primary key enforces "at most one credential" without extra locking.</summary>
    public static readonly Guid SingletonId = new("00000000-0000-0000-0000-000000000001");

    public Guid Id { get; init; } = SingletonId;

    /// <summary>PBKDF2 hash of the password, formatted as <c>"{iterations}.{saltBase64}.{hashBase64}"</c>.</summary>
    public string PasswordHash { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}
