using Extensions;
using Services.Notifications.Database;

namespace Services.Notifications.Entities;

/// <summary>
/// API-facing representation of a configured notification extension, returned to clients from the notifications endpoints.
/// </summary>
public sealed record NotificationExtension
{
    /// <summary>The unique identifier of the notification extension instance.</summary>
    public required Guid Id { get; init; }
    /// <summary>The user-assigned display name of the extension instance.</summary>
    public required string Name { get; init; }
    /// <summary>The notification channel type this extension represents.</summary>
    public required NotificationExtensionType Type { get; init; }
}