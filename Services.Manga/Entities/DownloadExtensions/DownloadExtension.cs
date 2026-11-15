namespace Services.Manga.Entities.DownloadExtensions;

/// <summary>
/// A download source extension (e.g. MangaDex) that manga/chapters can be attributed to.
/// </summary>
public sealed record DownloadExtension
{
    /// <summary>The stable identifier of the download extension, matching <c>IExtension.Identifier</c> in the Extensions project.</summary>
    public required Guid DownloadExtensionsId { get; init; }

    /// <summary>The human-readable name of the download extension.</summary>
    public required string Name { get; init; }

    /// <summary>
    /// URL to the download extension's icon/logo.
    /// </summary>
    public required string IconUrl { get; init; }
}
