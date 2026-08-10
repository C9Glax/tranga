namespace Common.Services.Events.Events;

/// <summary>Published when a chapter has finished downloading and its file has been written to disk.</summary>
/// <param name="FilePath">Absolute path of the downloaded chapter file.</param>
/// <param name="MangaId">Id of the manga the chapter belongs to.</param>
/// <param name="Series">Name of the series/manga the chapter belongs to.</param>
/// <param name="Chapter">Chapter number.</param>
/// <param name="Title">Chapter title, if any.</param>
/// <param name="Volume">Volume number the chapter belongs to, if any.</param>
public record ChapterDownloadedEvent(string FilePath, Guid MangaId, string Series, string Chapter, string? Title, string? Volume)
    : TrangaEvent;