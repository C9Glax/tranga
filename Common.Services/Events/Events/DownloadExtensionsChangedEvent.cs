namespace Common.Services.Events.Events;

/// <summary>
/// Published when the set of download extensions has changed (a custom source was created, edited or deleted), so
/// that every service reloads its own <c>DownloadExtensionsCollection</c>.
/// </summary>
public record DownloadExtensionsChangedEvent : TrangaEvent;
