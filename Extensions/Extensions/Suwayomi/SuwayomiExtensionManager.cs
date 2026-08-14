namespace Extensions.Extensions.Suwayomi;

/// <summary>
/// The public surface services use to inspect and manage the extensions installed on the Suwayomi sidecar.
/// Keeps the GraphQL details of <see cref="SuwayomiClient"/> inside this assembly.
/// </summary>
public static class SuwayomiExtensionManager
{
    /// <summary>Whether the sidecar is switched on for this deployment.</summary>
    public static bool IsEnabled => SuwayomiSource.IsAvailable;

    /// <summary>Reports whether the sidecar is configured and answering, along with the number of sources it exposes.</summary>
    public static async Task<SuwayomiStatus> GetStatusAsync(CancellationToken ct)
    {
        if (!IsEnabled)
            return new SuwayomiStatus(false, false, null, null, 0);

        if (await SuwayomiClient.GetAboutAsync(ct) is not { } about)
            return new SuwayomiStatus(true, false, null, null, 0);

        SuwayomiSourceDto[] sources = await SuwayomiClient.GetSourcesAsync(ct) ?? [];
        return new SuwayomiStatus(true, true, about.Name, about.Version, sources.Length);
    }

    /// <summary>
    /// The extension catalogue.
    /// </summary>
    /// <param name="refresh">
    /// When <see langword="true"/>, the configured extension stores (keiyoushi) are re-read first. That hits the
    /// network and is slow, so the frontend only asks for it on an explicit refresh.
    /// </param>
    /// <param name="ct"></param>
    /// <returns><see langword="null"/> when the sidecar is disabled or unreachable.</returns>
    public static async Task<SuwayomiExtensionInfo[]?> GetExtensionsAsync(bool refresh, CancellationToken ct)
    {
        if (!IsEnabled)
            return null;

        SuwayomiExtensionDto[]? extensions = refresh
            ? await SuwayomiClient.FetchExtensionsAsync(ct)
            : await SuwayomiClient.GetExtensionsAsync(ct);

        return extensions?.Select(ToInfo).ToArray();
    }

    /// <summary>Installs an extension by package name.</summary>
    public static Task<bool> InstallAsync(string pkgName, CancellationToken ct) => SetStateAsync(pkgName, SuwayomiExtensionAction.Install, ct);

    /// <summary>Updates an already-installed extension to the newest version in its store.</summary>
    public static Task<bool> UpdateAsync(string pkgName, CancellationToken ct) => SetStateAsync(pkgName, SuwayomiExtensionAction.Update, ct);

    /// <summary>Uninstalls an extension, removing all of its sources.</summary>
    public static Task<bool> UninstallAsync(string pkgName, CancellationToken ct) => SetStateAsync(pkgName, SuwayomiExtensionAction.Uninstall, ct);

    /// <summary>The sources of every installed extension, paired with the Tranga extension id each one is registered under.</summary>
    /// <returns><see langword="null"/> when the sidecar is disabled or unreachable.</returns>
    public static async Task<SuwayomiSourceInfo[]?> GetSourcesAsync(CancellationToken ct)
    {
        if (!IsEnabled)
            return null;

        return (await SuwayomiClient.GetSourcesAsync(ct))
            ?.Select(source => new SuwayomiSourceInfo(
                source.Id,
                SuwayomiSource.IdentifierFor(source.Id),
                string.IsNullOrWhiteSpace(source.DisplayName) ? source.Name : source.DisplayName,
                source.Lang,
                SuwayomiClient.ToGatewayUrl(source.IconUrl),
                source.HomeUrl ?? string.Empty,
                source.IsNsfw))
            .ToArray();
    }

    private static async Task<bool> SetStateAsync(string pkgName, SuwayomiExtensionAction action, CancellationToken ct)
    {
        if (!IsEnabled)
            return false;
        return await SuwayomiClient.SetExtensionStateAsync(pkgName, action, ct) is not null;
    }

    private static SuwayomiExtensionInfo ToInfo(SuwayomiExtensionDto extension) => new(
        extension.PkgName,
        extension.Name,
        extension.Lang,
        SuwayomiClient.ToGatewayUrl(extension.IconUrl),
        extension.VersionName,
        extension.IsNsfw,
        extension.IsInstalled,
        extension.IsObsolete,
        extension.HasUpdate);
}

/// <summary>Reachability and version of the Suwayomi sidecar.</summary>
/// <param name="Enabled">Whether <c>ENABLE_SUWAYOMI</c> is set for this deployment.</param>
/// <param name="Reachable">Whether the sidecar answered. False while enabled means the container is missing or still starting.</param>
/// <param name="ServerName">Sidecar product name, when reachable.</param>
/// <param name="ServerVersion">Sidecar version, when reachable.</param>
/// <param name="InstalledSourceCount">Number of sources currently exposed by installed extensions.</param>
public sealed record SuwayomiStatus(bool Enabled, bool Reachable, string? ServerName, string? ServerVersion, int InstalledSourceCount);

/// <summary>An extension offered by, or installed from, a configured extension store.</summary>
/// <param name="PkgName">Android package name; the identifier used to install, update and uninstall.</param>
/// <param name="Name">Display name of the extension.</param>
/// <param name="Lang">Tachiyomi language code, e.g. <c>en</c>, <c>pt-BR</c>, <c>all</c>.</param>
/// <param name="IconUrl">Gateway-relative url of the extension's icon.</param>
/// <param name="VersionName">Version currently installed, or offered by the store when not installed.</param>
/// <param name="IsNsfw">Whether the extension is flagged as NSFW. Its sources return nothing while <c>AllowNSFW</c> is off.</param>
/// <param name="IsInstalled">Whether the extension is installed on the sidecar.</param>
/// <param name="IsObsolete">Whether the extension is no longer offered by any configured store.</param>
/// <param name="HasUpdate">Whether a newer version is available.</param>
public sealed record SuwayomiExtensionInfo(
    string PkgName,
    string Name,
    string Lang,
    string IconUrl,
    string VersionName,
    bool IsNsfw,
    bool IsInstalled,
    bool IsObsolete,
    bool HasUpdate);

/// <summary>A source exposed by an installed extension, and the Tranga download-extension it is registered as.</summary>
/// <param name="SourceId">The Tachiyomi source id.</param>
/// <param name="ExtensionId">The Tranga extension identifier derived from <paramref name="SourceId"/>.</param>
/// <param name="Name">Display name of the source, normally including its language.</param>
/// <param name="Lang">Tachiyomi language code.</param>
/// <param name="IconUrl">Gateway-relative url of the source's icon.</param>
/// <param name="HomeUrl">The source's own website.</param>
/// <param name="IsNsfw">Whether the source is flagged as NSFW.</param>
public sealed record SuwayomiSourceInfo(
    string SourceId,
    Guid ExtensionId,
    string Name,
    string Lang,
    string IconUrl,
    string HomeUrl,
    bool IsNsfw);
