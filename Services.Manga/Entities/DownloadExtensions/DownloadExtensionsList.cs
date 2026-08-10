namespace Services.Manga.Entities.DownloadExtensions;

/// <summary>
/// The set of download extensions available to the manga service, exposed as an API-friendly list.
/// </summary>
public sealed record DownloadExtensionsList
{
    /// <summary>The available download extensions.</summary>
    public IDownloadExtension[] Extensions { get; init; } =
    [
        new MangaDex(),
        new WeebCentral(),
        new AsuraScans(),
        new MangaPlus()
    ];
}
