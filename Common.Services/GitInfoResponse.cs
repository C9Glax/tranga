namespace Common.Services;

/// <summary>Build commit/version/branch info for this service, sourced from devlooped/GitInfo at build time.</summary>
/// <param name="Sha">Full git commit SHA.</param>
/// <param name="Commit">Short (8-character) git commit SHA.</param>
/// <param name="Branch">Git branch the build was produced from.</param>
/// <param name="Version">SemVer derived from the nearest git tag and commit count.</param>
/// <param name="CommitDate">Timestamp of the commit the build was produced from.</param>
/// <param name="IsDirty">Whether the working tree had uncommitted changes at build time.</param>
public sealed record GitInfoResponse(string Sha, string Commit, string Branch, string Version, string CommitDate, bool IsDirty);
