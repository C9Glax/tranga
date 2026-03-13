namespace DownloadExtensions.Data;

/// <summary>
/// The Manga-Info returned by an Extension
/// </summary>
/// <param name="Title">Title of the Manga</param>
/// <param name="Url">Url to the page of the Manga</param>
/// <param name="Cover">Cover-Image of the Manga</param>
/// <param name="Description">Description of the Manga</param>
public record Manga<T>(
    string Title,
    string Url,
    string Identifier,
    MemoryStream Cover,
    string? Description = null
) where T : IExtension<T>;