namespace Common.Services.Events.Events;

/// <summary>Published when a chapter has been deleted, along with its downloaded file (if any).</summary>
/// <param name="MangaId">Id of the manga the chapter belonged to.</param>
public record ChapterDeletedEvent(Guid MangaId) : TrangaEvent;
