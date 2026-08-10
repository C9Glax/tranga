namespace Common.Services.Events.Events;

/// <summary>Published when a manga's download link (source URL used to fetch chapters) has been added, changed, or removed.</summary>
/// <param name="DownloadLinkId">Id of the download link that was modified.</param>
public record DownloadLinkModifiedEvent(Guid DownloadLinkId) : TrangaEvent;