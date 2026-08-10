using Extensions;

namespace Services.Notifications.Database;

/// <summary>
/// Base persisted record for a configured notification extension (e.g. Naprise-backed channels such as Gotify, Telegram, Discord, Ntfy.sh).
/// </summary>
/// <param name="Name">The user-assigned display name of the extension instance.</param>
/// <param name="Type">The notification channel type this extension represents.</param>
public abstract record DbNotificationExtension(string Name, NotificationExtensionType Type)
{
    /// <summary>The unique identifier of this notification extension instance.</summary>
    public Guid Id { get; init; } = Guid.CreateVersion7();

    /// <inheritdoc cref="Type" />
    public NotificationExtensionType Type { get; init; } = Type;

    /// <inheritdoc cref="Name" />
    public string Name { get; init; } = Name;
}