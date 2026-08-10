namespace Common.Services.Events.Events;

/// <summary>Published when a manga's metadata has been updated and dependent services (e.g. library sync) should react.</summary>
/// <param name="MangaId">Id of the manga that was updated.</param>
public record MangaUpdatedEvent(Guid MangaId) : TrangaEvent;
