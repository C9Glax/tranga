namespace Common.Database.Auth;

/// <summary>
/// Grants an <see cref="DbApiKey"/> access to. Only one scope exists today; the enum exists so more can be
/// added later without a breaking schema change.
/// </summary>
public enum ApiKeyScope
{
    /// <summary>Full access to every endpoint, identical to a logged-in session.</summary>
    All
}
