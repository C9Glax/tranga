namespace Common.Services.Events.Events;

public record DownloadLinkModifiedEvent(Guid DownloadLinkId) : TrangaEvent;