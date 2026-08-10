namespace Common.Services.Events.Events;

/// <summary>Published when a Manga has been merged into another and should be removed from dependent services (e.g. library mappings).</summary>
/// <param name="SourceMangaId">Id of the Manga that was merged away and deleted.</param>
/// <param name="TargetMangaId">Id of the surviving Manga the source was merged into.</param>
public record MangaMergedEvent(Guid SourceMangaId, Guid TargetMangaId) : TrangaEvent;
