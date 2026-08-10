namespace Services.Manga.Entities;

/// <summary>
/// API/transport representation of a chapter, extending <see cref="Chapter"/> with whether it has been downloaded.
/// </summary>
public sealed record MangaChapter : Chapter
{
    /// <summary>Whether the chapter's file has already been downloaded and stored.</summary>
    public required bool IsDownloaded { get; init; }
}
