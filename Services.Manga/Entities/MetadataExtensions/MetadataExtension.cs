namespace Services.Manga.Entities.MetadataExtensions;

/// <summary>
/// A metadata source extension (e.g. MangaDex, MangaUpdates) that manga metadata entries can be attributed to.
/// </summary>
public sealed record MetadataExtension
{
    /// <summary>The stable identifier of the metadata extension, matching <c>IExtension.Identifier</c> in the Extensions project.</summary>
    public required Guid MetadataExtensionId { get; init; }

    /// <summary>The human-readable name of the metadata extension.</summary>
    public required string Name { get; init; }

    /// <summary>URL to the metadata extension's icon/logo.</summary>
    public required string IconUrl { get; init; }
}
