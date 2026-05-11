using Extensions.Data;

namespace Extensions;

public interface INotificationExtension
{
    /// <summary>
    /// Sends a notification to the extension
    /// </summary>
    /// <param name="notification">Notification to send</param>
    /// <param name="ct">The Cancellation Token for the Task</param>
    /// <returns>A Task representing the long running operation.</returns>
    public Task SendNotification(Notification notification, CancellationToken ct);
}

public enum NotificationExtensionType
{
    Naprise,
    Discord,
    Gotify,
    NtfySh,
    Telegram
}