namespace DownloadExtensions.Data;

/// <summary>
/// The Manga-Info returned by an Extension
/// </summary>
/// <param name="Title">Title of the Manga</param>
/// <param name="Url">Url to the page of the Manga</param>
/// <param name="Cover">Cover-Image of the Manga</param>
/// <param name="Description">Description of the Manga</param>
public record MangaInfo(
    Guid ExtensionIdentifier,
    string Title,
    string Url,
    string Identifier,
    MemoryStream Cover,
    string? Description = null
);