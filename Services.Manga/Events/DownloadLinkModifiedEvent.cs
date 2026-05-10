using Common.Services.Events;

namespace Services.Manga.Events;

public record DownloadLinkModifiedEvent(Guid DownloadLinkId) : TrangaEvent;