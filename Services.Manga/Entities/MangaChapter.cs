namespace Services.Manga.Entities;

public sealed record MangaChapter : Chapter
{
    public required bool IsDownloaded { get; init; }
}
