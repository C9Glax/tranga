namespace Common.Services.Events.Events;

public record ChapterDownloadedEvent(string FilePath, string Series, string Chapter, string? Title, string? Volume)
    : TrangaEvent;