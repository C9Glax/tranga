namespace Services.Manga.Entities.MetadataExtensions;

/// <summary>
/// The set of metadata extensions available to the manga service, exposed as an API-friendly list.
/// </summary>
public sealed record MetadataExtensionsList
{
    /// <summary>The available metadata extensions.</summary>
    public IMetadataExtension[] Extensions { get; init; } =
    [
        new MangaDex(),
        new MangaUpdates(),
        new MangaPlus()
    ];
}
