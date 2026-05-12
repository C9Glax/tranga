namespace Common.Services.Events.Events;

public record ChapterDownloadedEvent(string Series, string Chapter, string? Title, string? Volume) : TrangaEvent;