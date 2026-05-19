namespace Common.Services.Events.Events;

public record ChapterDownloadedEvent(string FilePath, Guid MangaId, string Series, string Chapter, string? Title, string? Volume)
    : TrangaEvent;