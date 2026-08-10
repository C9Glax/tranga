using Extensions;

namespace Services.Notifications.Database;

/// <summary>
/// Persisted notification extension backed by the Naprise library, addressed by a single service URL
/// (used for Gotify, Telegram, Discord, and Ntfy.sh, which all encode their channel-specific configuration into this URL).
/// </summary>
/// <param name="Name">The user-assigned display name of the extension instance.</param>
/// <param name="ServiceUrl">The Naprise service URL identifying the notification channel and its configuration.</param>
public record DbNapriseExtension(string Name, string ServiceUrl) : DbNotificationExtension(Name, NotificationExtensionType.Naprise)
{
    /// <inheritdoc cref="ServiceUrl" />
    public string ServiceUrl { get; init; } = ServiceUrl;
}