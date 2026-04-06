namespace API.Entities.MetadataExtensions;

public sealed record MetadataExtensionsList
{
    public IMetadataExtension[] Extensions { get; init; } =
    [
        new MangaDex(),
        new MangaUpdates()
    ];
}