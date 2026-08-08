namespace Common.Services.Events.Events;

public record MangaUpdatedEvent(Guid MangaId) : TrangaEvent;
