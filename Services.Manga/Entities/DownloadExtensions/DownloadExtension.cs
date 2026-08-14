namespace Services.Manga.Entities.DownloadExtensions;

/// <summary>
/// A download source extension (e.g. MangaDex, WeebCentral, or any source installed on the Suwayomi sidecar) that
/// manga/chapters can be attributed to.
/// </summary>
public sealed record DownloadExtension
{
    /// <summary>The stable identifier of the download extension, matching <c>IExtension.Identifier</c> in the Extensions project.</summary>
    public required Guid DownloadExtensionsId { get; init; }

    /// <summary>The human-readable name of the download extension.</summary>
    public required string Name { get; init; }

    /// <summary>
    /// URL to the download extension's icon/logo. Absolute for the built-in extensions; relative to the gateway
    /// (<c>/suwayomi/...</c>) for sources served through the Suwayomi sidecar.
    /// </summary>
    public required string IconUrl { get; init; }

    /// <summary>
    /// Whether this extension is provided by the Suwayomi sidecar rather than compiled into Tranga. Sidecar-backed
    /// extensions appear and disappear as the user installs and removes extensions under Settings -> Sources.
    /// </summary>
    public required bool IsSuwayomiSource { get; init; }
}
